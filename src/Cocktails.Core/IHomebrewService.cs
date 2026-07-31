using Cocktails.Core.Models;

namespace Cocktails.Core;

/// <summary>
/// Couche d'accès à Homebrew. Isole entièrement le reste de l'application de la
/// CLI <c>brew</c> : la couche UI (Avalonia/MVVM) ne dépend que de cette interface,
/// jamais de <see cref="System.Diagnostics.Process"/> ni du format de sortie de brew.
/// </summary>
public interface IHomebrewService
{
    /// <summary>Liste les packages actuellement installés (formulae et casks).</summary>
    Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>Recherche des packages par nom / mot-clé.</summary>
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Liste les packages installés pour lesquels une mise à jour est disponible.</summary>
    Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default);

    /// <summary>Actualise l'index des formules/casks (<c>brew update</c>).</summary>
    Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Récupère le détail enrichi d'un package (description, dépendances, homepage…).</summary>
    Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Épingle une formula à sa version (<c>brew pin</c>).</summary>
    Task PinAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Désépingle une formula (<c>brew unpin</c>).</summary>
    Task UnpinAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Paquets installés qui dépendent de <paramref name="name"/> (<c>brew uses --installed</c>).</summary>
    Task<IReadOnlyList<string>> GetDependentsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Formulae installées « à la racine » (aucun autre paquet n'en dépend, <c>brew leaves</c>).</summary>
    Task<IReadOnlyList<string>> GetLeavesAsync(CancellationToken cancellationToken = default);

    /// <summary>Environnement Homebrew (version, préfixe, cache).</summary>
    Task<BrewEnvironment> GetEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>État de la télémétrie Homebrew (<c>brew analytics</c>).</summary>
    Task<bool> GetAnalyticsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>Active/désactive la télémétrie Homebrew (<c>brew analytics on/off</c>).</summary>
    Task SetAnalyticsAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Installe un package. <paramref name="output"/> reçoit le log brew en direct.</summary>
    Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Réinstalle un package (<c>brew reinstall</c>).</summary>
    Task ReinstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Désinstalle un package installé. <paramref name="output"/> reçoit le log brew.</summary>
    Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour un package précis, ou tous les packages obsolètes si
    /// <paramref name="name"/> est <c>null</c>. <paramref name="output"/> reçoit le log brew.
    /// </summary>
    Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Nettoie le cache et les anciennes versions (<c>brew cleanup</c>).</summary>
    Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Retire les dépendances installées automatiquement et devenues inutiles (<c>brew autoremove</c>).</summary>
    Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Diagnostic de l'installation (<c>brew doctor</c>). Ne lève pas sur avertissements.</summary>
    Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formules installées auxquelles il manque des dépendances (<c>brew missing</c>).
    /// Liste vide = tout est complet. Ne lève pas si des manques sont trouvés.
    /// </summary>
    Task<IReadOnlyList<MissingDependency>> GetMissingAsync(CancellationToken cancellationToken = default);

    /// <summary>Exporte l'installé dans un Brewfile (<c>brew bundle dump</c>).</summary>
    Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Installe depuis un Brewfile (<c>brew bundle install</c>).</summary>
    Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les services gérés par Homebrew (<c>brew services list</c>).</summary>
    Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Démarre un service (<c>brew services start</c>).</summary>
    Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Arrête un service (<c>brew services stop</c>).</summary>
    Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Redémarre un service (<c>brew services restart</c>).</summary>
    Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les taps installés (<c>brew tap-info --installed --json</c>).</summary>
    Task<IReadOnlyList<BrewTap>> GetTapsAsync(CancellationToken cancellationToken = default);

    /// <summary>Ajoute un tap (<c>brew tap</c>).</summary>
    Task AddTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Retire un tap (<c>brew untap</c>).</summary>
    Task RemoveTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Fait confiance à un tap non officiel (<c>brew trust</c>).</summary>
    Task TrustTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);
}
