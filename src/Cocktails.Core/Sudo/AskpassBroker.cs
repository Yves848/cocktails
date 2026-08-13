using System.Net.Sockets;
using System.Text;

namespace Cocktails.Core.Sudo;

/// <summary>Contexte d'une demande de mot de passe adressée à l'interface.</summary>
/// <param name="IsRetry">Vrai si la saisie précédente a été refusée par sudo.</param>
public sealed record PasswordPrompt(bool IsRetry);

/// <summary>Réponse de l'interface à une demande de mot de passe.</summary>
/// <param name="Password">Mot de passe saisi.</param>
/// <param name="Remember">Le garder en mémoire pour les demandes suivantes.</param>
public sealed record PasswordReply(string Password, bool Remember);

/// <summary>
/// Demande un mot de passe à l'utilisateur. Rend <c>null</c> si l'utilisateur annule.
/// </summary>
public delegate Task<PasswordReply?> PasswordPromptHandler(
    PasswordPrompt prompt, CancellationToken cancellationToken);

/// <summary>
/// Courtier <c>SUDO_ASKPASS</c> : permet à <c>brew</c> d'obtenir les droits administrateur
/// sans terminal. Homebrew ajoute <c>-A</c> à ses appels <c>sudo</c> dès que
/// <c>SUDO_ASKPASS</c> est défini ; sudo lance alors le programme désigné et lit le mot de
/// passe sur sa sortie standard.
/// <para>
/// Le helper généré ici est un script minimal qui relaie une socket UNIX vers sa sortie.
/// C'est l'application qui écoute cette socket : à chaque demande de sudo elle interroge
/// l'interface (<see cref="Handler"/>) et renvoie le mot de passe. Le secret ne transite
/// donc ni par la ligne de commande, ni par l'environnement, ni par un fichier — et il
/// n'est jamais écrit sur disque.
/// </para>
/// </summary>
public sealed class AskpassBroker : ISudoAskpass, IAsyncDisposable
{
    /// <summary>Relais utilisé par le helper. Absent (macOS très ancien) → askpass désactivé.</summary>
    private const string NetcatPath = "/usr/bin/nc";

    private readonly string _directory;
    private readonly string _socketPath;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Lock _gate = new();

    private Socket? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    private string? _remembered;
    private DateTimeOffset _rememberedAt;
    private Timer? _purgeTimer;

    /// <summary>Mots de passe déjà servis depuis le début de l'opération brew en cours.</summary>
    private int _servedInOperation;

    /// <param name="directory">
    /// Dossier privé (créé en 0700) où sont posés le script helper et la socket.
    /// </param>
    /// <param name="clock">Horloge (injectable pour tester la péremption du cache).</param>
    public AskpassBroker(string directory, Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);

        if (directory.Contains('\''))
        {
            // Le chemin est injecté tel quel dans le script shell, entre apostrophes.
            throw new ArgumentException("Le dossier askpass ne peut pas contenir d'apostrophe.", nameof(directory));
        }

        _directory = directory;
        _socketPath = Path.Combine(directory, "askpass.sock");
    }

    /// <summary>
    /// Chemin du script à exporter dans <c>SUDO_ASKPASS</c>, ou <c>null</c> tant que
    /// <see cref="StartAsync"/> n'a pas réussi (le cas échéant, on n'exporte rien et brew
    /// se comporte comme avant).
    /// </summary>
    public string? HelperPath { get; private set; }

    /// <summary>Demande le mot de passe à l'interface. Non branché → toute demande est refusée.</summary>
    public PasswordPromptHandler? Handler { get; set; }

    /// <summary>
    /// Durée pendant laquelle un mot de passe « à retenir » survit sans nouvel appel sudo.
    /// Passé ce délai il est effacé de la mémoire — l'application pouvant tourner des jours
    /// en arrière-plan, le garder indéfiniment reviendrait à le stocker.
    /// <see cref="Timeout.InfiniteTimeSpan"/> le garde toute la session.
    /// </summary>
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>Vrai si un mot de passe encore valide est gardé en mémoire.</summary>
    public bool HasRememberedPassword
    {
        get
        {
            lock (_gate)
            {
                return Remembered() is not null;
            }
        }
    }

    /// <summary>
    /// Ouvre une nouvelle opération brew : remet à zéro le compteur de tentatives, dont on
    /// déduit qu'un mot de passe a été refusé (cf. <see cref="ServeAsync"/>).
    /// </summary>
    public void BeginOperation()
    {
        lock (_gate)
        {
            _servedInOperation = 0;
        }
    }

    /// <summary>Efface le mot de passe gardé en mémoire.</summary>
    public void ForgetPassword()
    {
        lock (_gate)
        {
            Forget();
        }
    }

    /// <summary>Installe le script helper et met la socket en écoute.</summary>
    public Task StartAsync()
    {
        if (!File.Exists(NetcatPath))
        {
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(_directory);
        Restrict(_directory, executable: true);

        File.Delete(_socketPath);
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        _listener.Listen(backlog: 4);
        Restrict(_socketPath, executable: false);

        var helper = Path.Combine(_directory, "askpass.sh");
        File.WriteAllText(helper, BuildHelperScript(_socketPath));
        Restrict(helper, executable: true);
        HelperPath = helper;

        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        return Task.CompletedTask;
    }

    /// <summary>Réserve l'accès au propriétaire (0600, ou 0700 pour un exécutable/dossier).</summary>
    private static void Restrict(string path, bool executable)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        File.SetUnixFileMode(path, executable ? mode | UnixFileMode.UserExecute : mode);
    }

    /// <summary>
    /// Script lu par sudo. <c>-d</c> empêche le relais de lire l'entrée standard (celle de
    /// sudo, qu'il garderait ouverte) ; une sortie vide vaut annulation, et le code de
    /// retour non nul fait abandonner sudo au lieu de réessayer trois fois.
    /// </summary>
    private static string BuildHelperScript(string socketPath) =>
        $"""
        #!/bin/sh
        # Généré par Cocktails — lu par sudo via SUDO_ASKPASS. Ne pas modifier.
        password=$({NetcatPath} -U -d '{socketPath}') || exit 1
        [ -n "$password" ] || exit 1
        printf '%s\n' "$password"
        """;

    /// <summary>
    /// Sert les demandes une à une : sudo n'en émet jamais deux en parallèle, et le faire
    /// séquentiellement évite d'ouvrir deux boîtes de saisie en même temps.
    /// </summary>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = await _listener!.AcceptAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }

            using (client)
            {
                await ServeAsync(client, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ServeAsync(Socket client, CancellationToken cancellationToken)
    {
        var password = await ResolvePasswordAsync(cancellationToken).ConfigureAwait(false);

        if (password is not null)
        {
            await client
                .SendAsync(Encoding.UTF8.GetBytes(password), SocketFlags.None, cancellationToken)
                .ConfigureAwait(false);
        }

        // Annulation : on ferme sans rien écrire — le helper sort en erreur, sudo abandonne
        // au lieu d'enchaîner ses trois tentatives.
        client.Shutdown(SocketShutdown.Both);
    }

    /// <summary>
    /// Mot de passe à servir : celui gardé en mémoire s'il est encore valide, sinon celui
    /// que l'utilisateur saisit. <c>null</c> = annulation.
    /// </summary>
    private async Task<string?> ResolvePasswordAsync(CancellationToken cancellationToken)
    {
        bool isRetry;
        lock (_gate)
        {
            // sudo ne redemande le mot de passe dans une même opération que s'il vient de
            // le refuser : celui qu'on garde est donc faux, on le jette et on le signale.
            isRetry = _servedInOperation > 0;
            if (isRetry)
            {
                Forget();
            }
            else if (Remembered() is { } cached)
            {
                _rememberedAt = _clock();   // péremption glissante : comptée depuis le dernier usage
                _servedInOperation++;
                return cached;
            }
        }

        if (Handler is null)
        {
            return null;
        }

        var reply = await Handler(new PasswordPrompt(isRetry), cancellationToken).ConfigureAwait(false);
        if (reply is null)
        {
            return null;
        }

        lock (_gate)
        {
            _servedInOperation++;
            if (reply.Remember)
            {
                Remember(reply.Password);
            }
        }

        return reply.Password;
    }

    /// <summary>Mot de passe en mémoire, ou <c>null</c> s'il est absent ou périmé (auquel cas il est effacé).</summary>
    private string? Remembered()
    {
        // Durée négative (Timeout.InfiniteTimeSpan) = « toute la session », jamais périmé.
        if (_remembered is not null
            && CacheLifetime >= TimeSpan.Zero
            && _clock() - _rememberedAt > CacheLifetime)
        {
            Forget();
        }

        return _remembered;
    }

    private void Remember(string password)
    {
        _remembered = password;
        _rememberedAt = _clock();

        // La vérification à l'usage suffirait à ne jamais servir un mot de passe périmé,
        // mais elle le laisserait en mémoire ; ce minuteur l'en retire vraiment.
        _purgeTimer?.Dispose();
        _purgeTimer = new Timer(_ => ForgetPassword(), null, CacheLifetime, Timeout.InfiniteTimeSpan);
    }

    private void Forget()
    {
        _remembered = null;
        _purgeTimer?.Dispose();
        _purgeTimer = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        _listener?.Dispose();

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts?.Dispose();
        ForgetPassword();

        try
        {
            File.Delete(_socketPath);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }
}
