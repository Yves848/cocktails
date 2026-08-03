using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Shell de l'application : navigation latérale + écran actif. Chaque écran est un
/// <see cref="ScreenViewModel"/> autonome ; le shell se contente de commuter l'écran
/// courant et d'exposer son état pour la barre d'état.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    // Icônes tracées au trait (viewBox 24×24, style Lucide).
    private const string IconInstalled =
        "M21 8v8a2 2 0 0 1-1 1.73l-7 4a2 2 0 0 1-2 0l-7-4A2 2 0 0 1 3 16V8a2 2 0 0 1 1-1.73l7-4a2 2 0 0 1 2 0l7 4A2 2 0 0 1 21 8z M3.3 7 12 12l8.7-5 M12 22V12";
    private const string IconSearch =
        "M11 3a8 8 0 1 0 0 16 8 8 0 0 0 0-16z M21 21l-4.35-4.35";
    private const string IconUpdates =
        "M12 3v12 M8 11l4 4 4-4 M3 21h18";
    private const string IconMaintenance =
        "M14.7 6.3a4 4 0 0 0-5.4 5.4L3 18v3h3l6.3-6.3a4 4 0 0 0 5.4-5.4l-2.6 2.6-2-2 2.6-2.6z";
    private const string IconServices =
        "M22 12h-4l-3 9L9 3l-3 9H2";
    private const string IconTaps =
        "M6 3v12 M18 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M6 21a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M15 6a9 9 0 0 1-9 9";
    private const string IconSettings =
        "M4 21v-7 M4 10V3 M12 21v-9 M12 8V3 M20 21v-5 M20 12V3 M1 14h6 M9 8h6 M17 16h6";
    private const string IconHelp =
        "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3 M12 17h.01";

    private readonly NavItem _updatesNav;
    private readonly IHomebrewService _homebrew;

    // Historique du terminal (commandes exécutées) + navigation ↑/↓.
    private readonly List<string> _history = [];
    private int _historyIndex;          // == _history.Count : position « brouillon »
    private string _draft = string.Empty;
    // Catalogue (complétion + détection cask) et noms installés (complétion ciblée),
    // chargés paresseusement à l'ouverture du terminal.
    private IReadOnlyList<string>? _names;             // formulae + casks (complétion générale)
    private IReadOnlyList<string>? _installedNames;
    private HashSet<string>? _formulaSet;              // pour distinguer un cask « pur »
    private HashSet<string>? _caskSet;
    private Task? _loadTask;             // chargement du catalogue mémoïsé (awaitable une fois)
    private bool _suppressSuggestions;   // vrai pendant un set programmatique (historique / accept)
    private List<string> _lastResults = [];   // résultats de la commande précédente (enchaînement)

    public MainViewModel(IHomebrewService homebrew, AppSettings? settings = null, UpdateMonitor? monitor = null,
        INotifier? notifier = null)
    {
        _homebrew = homebrew;
        settings ??= new AppSettings();
        Settings = settings;
        // Une couleur d'accent distincte par onglet (icônes uniques et colorées).
        _updatesNav = new NavItem("Nav.Updates", IconUpdates, new OutdatedViewModel(homebrew), "#F0B429");
        NavItems =
        [
            new NavItem("Nav.Installed", IconInstalled, new InstalledViewModel(homebrew, settings), "#2DD4BF"),
            new NavItem("Nav.Search", IconSearch, new SearchViewModel(homebrew), "#38BDF8"),
            _updatesNav,
            new NavItem("Nav.Maintenance", IconMaintenance, new MaintenanceViewModel(homebrew), "#A78BFA"),
            new NavItem("Nav.Services", IconServices, new ServicesViewModel(homebrew), "#43C07A"),
            new NavItem("Nav.Taps", IconTaps, new TapsViewModel(homebrew), "#F472B6"),
            new NavItem("Nav.Settings", IconSettings, new SettingsViewModel(homebrew, settings, notifier), "#94A3B8"),
            new NavItem("Nav.Help", IconHelp, new HelpViewModel(), "#FB923C"),
        ];

        // Le badge « Mises à jour » suit le compteur du moniteur.
        Monitor = monitor ?? new UpdateMonitor(homebrew, settings, new NullNotifier());
        Monitor.PropertyChanged += OnMonitorChanged;
        Monitor.OutdatedChanged += OnOutdatedChanged;
        _updatesNav.Count = Monitor.OutdatedCount;

        // Un upgrade fait depuis l'écran « Mises à jour » ne passe pas par le timer du
        // moniteur : on lui demande de recompter tout de suite (badge + liste à jour).
        if (_updatesNav.Screen is OutdatedViewModel outdatedVm)
        {
            outdatedVm.OutdatedSetChanged += (_, _) => _ = Monitor.CheckNowAsync();
        }

        SelectedNav = NavItems[0];
    }

    /// <summary>Réglages partagés (instance du shell) — expose le raccourci du terminal.</summary>
    public AppSettings Settings { get; }

    /// <summary>Moniteur de mises à jour (démarré par la racine de composition).</summary>
    public UpdateMonitor Monitor { get; }

    private void OnMonitorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpdateMonitor.OutdatedCount))
        {
            _updatesNav.Count = Monitor.OutdatedCount;
        }
    }

    /// <summary>
    /// Le moniteur a détecté un changement des paquets obsolètes : on invalide l'écran
    /// « Mises à jour » (rechargé à sa prochaine ouverture) et, s'il est affiché et
    /// inactif, on le recharge immédiatement — sans écraser une sélection en cours.
    /// </summary>
    private void OnOutdatedChanged(object? sender, EventArgs e)
    {
        if (_updatesNav.Screen is not OutdatedViewModel outdated)
        {
            return;
        }

        outdated.Invalidate();
        if (CurrentScreen == outdated && !outdated.IsBusy && !outdated.AnySelected)
        {
            _ = outdated.ActivateAsync();
        }
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public MainViewModel() : this(new DesignHomebrewService())
    {
    }

    public ObservableCollection<NavItem> NavItems { get; }

    [ObservableProperty]
    public partial NavItem? SelectedNav { get; set; }

    [ObservableProperty]
    public partial ScreenViewModel? CurrentScreen { get; set; }

    /// <summary>Terminal intégré déplié (sinon seule sa barre est visible).</summary>
    [ObservableProperty]
    public partial bool IsTerminalExpanded { get; set; }

    /// <summary>Déplie / replie le terminal intégré.</summary>
    [RelayCommand]
    private void ToggleTerminal() => IsTerminalExpanded = !IsTerminalExpanded;

    /// <summary>Vide la sortie du terminal de l'écran courant.</summary>
    [RelayCommand]
    private void ClearTerminal() => CurrentScreen?.OutputLog.Clear();

    /// <summary>Ligne de commande saisie dans le terminal intégré.</summary>
    [ObservableProperty]
    public partial string TerminalInput { get; set; } = string.Empty;

    /// <summary>Suggestions de complétion affichées dans le popup.</summary>
    public ObservableCollection<string> Suggestions { get; } = [];

    /// <summary>Popup de suggestions ouvert.</summary>
    [ObservableProperty]
    public partial bool IsSuggestionsOpen { get; set; }

    /// <summary>Suggestion surlignée (-1 = aucune).</summary>
    [ObservableProperty]
    public partial int SuggestionIndex { get; set; } = -1;

    /// <summary>
    /// Commandes brew proposées par la dernière sortie (ex. « brew install --cask … »),
    /// affichées en puces cliquables pour enchaîner directement.
    /// </summary>
    public ObservableCollection<string> SuggestedCommands { get; } = [];

    /// <summary>Exécute une commande proposée (puce) — enchaîne dessus.</summary>
    [RelayCommand]
    private async Task UseSuggestion(string command)
    {
        TerminalInput = command;
        await ExecuteTerminal();
    }

    /// <summary>
    /// Exécute la commande brew saisie sur l'écran courant (sortie streamée dans le
    /// terminal). Si la commande modifie l'état (install, uninstall…), l'écran est
    /// rechargé pour refléter le changement.
    /// </summary>
    [RelayCommand]
    private async Task ExecuteTerminal()
    {
        if (CurrentScreen is not { } screen || string.IsNullOrWhiteSpace(TerminalInput))
        {
            return;
        }

        // Une opération est déjà en cours : on ignore (sans effacer la saisie en cours).
        if (screen.IsBusy)
        {
            return;
        }

        var input = TerminalInput.Trim();
        TerminalInput = string.Empty;
        IsTerminalExpanded = true;

        // Historique (évite les doublons consécutifs), curseur remis en fin.
        if (input.Length > 0 && (_history.Count == 0 || _history[^1] != input))
        {
            _history.Add(input);
        }

        _historyIndex = _history.Count;
        _draft = string.Empty;
        SuggestedCommands.Clear();

        var parsed = BrewCommandLine.Parse(input);
        if (parsed is not null)
        {
            // `search <terme>` → écran Rechercher (tuiles enrichies) ; on mémorise les
            // résultats pour l'enchaînement (le prochain « install … » les proposera en tête).
            if (await TryRouteSearchAsync(parsed))
            {
                return;
            }

            // Catalogue prêt → ajoute --cask automatiquement si le paquet est un cask pur.
            await EnsureNamesAsync();
            parsed = AutoCask(parsed);
            input = string.Join(' ', parsed);
        }

        var startIndex = screen.OutputLog.Count;
        var args = await screen.RunTerminalCommandAsync(input);
        if (args is not null)
        {
            var newLines = screen.OutputLog.Skip(startIndex).ToList();
            DetectSuggestions(newLines);
            CaptureLastResults(args, newLines);
            if (BrewCommandLine.IsMutating(args))
            {
                screen.Invalidate();
                await screen.ActivateAsync();
            }
        }
    }

    // `search <terme>` : bascule sur l'écran Rechercher, lance la recherche et mémorise
    // les noms trouvés (_lastResults) pour prioriser la complétion de la commande suivante.
    private async Task<bool> TryRouteSearchAsync(string[] args)
    {
        if (!args[0].Equals("search", StringComparison.OrdinalIgnoreCase) || args.Length < 2
            || FindScreen<SearchViewModel>() is not { } search)
        {
            return false;
        }

        var query = string.Join(' ', args.Skip(1).Where(a => !a.StartsWith('-')));
        if (query.Length == 0)
        {
            return false;
        }

        SelectScreen("Nav.Search");
        search.SearchQuery = query;
        await search.SearchCommand.ExecuteAsync(null);
        _lastResults = search.Packages.Select(p => p.Name).ToList();
        return true;
    }

    // Mémorise les paquets listés par une commande (list, leaves, outdated…) pour
    // l'enchaînement ; vide le contexte pour les autres commandes.
    private void CaptureLastResults(string[] args, IEnumerable<string> outputLines)
    {
        string[] listing = ["list", "leaves", "outdated", "casks", "formulae", "deps", "uses"];
        if (!listing.Contains(args[0], StringComparer.OrdinalIgnoreCase))
        {
            _lastResults = [];
            return;
        }

        var names = new List<string>();
        foreach (var line in outputLines)
        {
            var token = line.Trim().Split(' ', 2)[0];
            if (token.Length > 0 && (_formulaSet?.Contains(token) == true || _caskSet?.Contains(token) == true))
            {
                names.Add(token);
            }
        }

        _lastResults = names;
    }

    // Repère dans la sortie les commandes brew proposées et les expose en puces.
    private void DetectSuggestions(IEnumerable<string> lines)
    {
        foreach (var command in BrewCommandLine.SuggestedCommands(lines))
        {
            SuggestedCommands.Add(command);
        }
    }

    private T? FindScreen<T>() where T : ScreenViewModel
        => NavItems.Select(n => n.Screen).OfType<T>().FirstOrDefault();

    /// <summary>Rappelle la commande précédente dans l'historique (↑).</summary>
    public void HistoryPrevious()
    {
        if (_history.Count == 0 || _historyIndex == 0)
        {
            return;
        }

        if (_historyIndex == _history.Count)
        {
            _draft = TerminalInput;   // sauvegarde la saisie en cours avant de plonger
        }

        _historyIndex--;
        TerminalInput = _history[_historyIndex];
    }

    /// <summary>Revient vers les commandes plus récentes, puis au brouillon (↓).</summary>
    public void HistoryNext()
    {
        if (_historyIndex >= _history.Count)
        {
            return;
        }

        _historyIndex++;
        TerminalInput = _historyIndex == _history.Count ? _draft : _history[_historyIndex];
    }

    /// <summary>Précharge les noms de paquets à la première ouverture du terminal.</summary>
    partial void OnIsTerminalExpandedChanged(bool value)
    {
        if (value)
        {
            _ = EnsureNamesAsync();
        }
    }

    /// <summary>Charge le catalogue une seule fois (mémoïsé) et le rend awaitable.</summary>
    private Task EnsureNamesAsync() => _loadTask ??= LoadNamesAsync();

    private async Task LoadNamesAsync()
    {
        try
        {
            var catalog = await _homebrew.GetCatalogAsync();
            _formulaSet = new HashSet<string>(catalog.Formulae, StringComparer.OrdinalIgnoreCase);
            _caskSet = new HashSet<string>(catalog.Casks, StringComparer.OrdinalIgnoreCase);
            _names = [.. catalog.Formulae, .. catalog.Casks];
            _installedNames = (await _homebrew.GetInstalledAsync()).Select(p => p.Name).ToList();
        }
        catch (Exception)
        {
            _names ??= [];
            _installedNames ??= [];
            _formulaSet ??= [];
            _caskSet ??= [];
        }

        UpdateSuggestions();   // au cas où l'utilisateur a déjà commencé à taper
    }

    /// <summary>
    /// Ajoute automatiquement <c>--cask</c> à une commande d'installation dont tous les
    /// paquets sont des casks « purs » (cask et non formula) — évite l'erreur brew
    /// « use --cask ». Ne touche rien si le flag est déjà présent ou en cas d'ambiguïté.
    /// </summary>
    private string[] AutoCask(string[] args)
    {
        if (args.Length < 2 || _caskSet is null || _formulaSet is null)
        {
            return args;
        }

        if (args[0] is not ("install" or "reinstall" or "upgrade"))
        {
            return args;
        }

        if (args.Any(a => a is "--cask" or "--formula"))
        {
            return args;
        }

        var names = args.Skip(1).Where(a => !a.StartsWith('-')).ToList();
        if (names.Count == 0 || !names.All(n => _caskSet.Contains(n) && !_formulaSet.Contains(n)))
        {
            return args;   // aucun nom, ou au moins un n'est pas un cask pur → on ne touche pas
        }

        return [args[0], "--cask", .. args[1..]];
    }

    private const int SuggestionCap = 60;

    // Découpe la saisie pour la complétion : partie fixe avant le mot courant, mot
    // courant, et les candidats correspondants (sous-commandes ou noms selon le contexte).
    private (string prefix, string word, List<string> matches) ComputeCompletion(string text)
    {
        var lastSpace = text.LastIndexOf(' ');
        var prefix = lastSpace < 0 ? string.Empty : text[..(lastSpace + 1)];
        var word = lastSpace < 0 ? text : text[(lastSpace + 1)..];

        var before = (lastSpace < 0 ? string.Empty : text[..lastSpace]).Trim();
        var firstWord = before.Length == 0 || before.Equals("brew", StringComparison.OrdinalIgnoreCase);
        var isOption = word.StartsWith('-');
        var isPackage = !isOption && !firstWord;

        // Mot vide : rien, SAUF en contexte paquet avec des résultats de la commande
        // précédente (ex. après « search git », « install » propose les paquets trouvés).
        if (word.Length == 0)
        {
            var recents = isPackage ? _lastResults.Distinct().Take(SuggestionCap).ToList() : [];
            return (prefix, word, recents);
        }

        IReadOnlyList<string>? pool;
        if (isOption)
        {
            // Complétion des options (« --versions », « --cask »…) de la sous-commande.
            pool = BrewCommandLine.OptionsFor(Subcommand(text));
        }
        else if (firstWord)
        {
            pool = BrewCommandLine.Subcommands;
        }
        else
        {
            pool = BrewCommandLine.CompletesInstalledOnly(Subcommand(text)) ? _installedNames : _names;
            if (pool is null)
            {
                _ = EnsureNamesAsync();   // chargement paresseux ; les suggestions suivront
                return (prefix, word, []);
            }
        }

        var filtered = pool.Where(n => n.StartsWith(word, StringComparison.OrdinalIgnoreCase));

        // En contexte paquet, les résultats de la commande précédente passent en tête.
        List<string> matches;
        if (isPackage && _lastResults.Count > 0)
        {
            var recent = new HashSet<string>(_lastResults, StringComparer.OrdinalIgnoreCase);
            matches = filtered
                .OrderByDescending(n => recent.Contains(n))
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            matches = filtered.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        return (prefix, word, matches);
    }

    // Première sous-commande de la saisie (après un « brew » éventuel).
    private static string Subcommand(string text)
    {
        var s = text.TrimStart();
        if (s.StartsWith("brew ", StringComparison.OrdinalIgnoreCase))
        {
            s = s[5..].TrimStart();
        }

        var sp = s.IndexOf(' ');
        return sp < 0 ? s : s[..sp];
    }

    // Recalcule le popup de suggestions à chaque frappe.
    partial void OnTerminalInputChanged(string value)
    {
        if (_suppressSuggestions)
        {
            return;
        }

        UpdateSuggestions();
    }

    private void UpdateSuggestions()
    {
        var (_, _, matches) = ComputeCompletion(TerminalInput);
        Suggestions.Clear();
        foreach (var m in matches.Take(SuggestionCap))
        {
            Suggestions.Add(m);
        }

        SuggestionIndex = -1;
        // Ouvre le popup dès qu'il y a des candidats (ComputeCompletion ne renvoie rien sur
        // un mot vide, sauf les résultats de la commande précédente en contexte paquet).
        IsSuggestionsOpen = Suggestions.Count > 0;
    }

    private void SetInputSilently(string text)
    {
        _suppressSuggestions = true;
        TerminalInput = text;
        _suppressSuggestions = false;
        CloseSuggestions();
    }

    public void SuggestionDown()
    {
        if (Suggestions.Count > 0)
        {
            SuggestionIndex = SuggestionIndex + 1 >= Suggestions.Count ? 0 : SuggestionIndex + 1;
        }
    }

    public void SuggestionUp()
    {
        if (Suggestions.Count > 0)
        {
            SuggestionIndex = SuggestionIndex <= 0 ? Suggestions.Count - 1 : SuggestionIndex - 1;
        }
    }

    public void CloseSuggestions()
    {
        IsSuggestionsOpen = false;
        SuggestionIndex = -1;
    }

    /// <summary>Accepte la suggestion surlignée (ou la première) dans la saisie.</summary>
    public void AcceptSuggestion()
    {
        var idx = SuggestionIndex >= 0 ? SuggestionIndex : 0;
        if (idx >= Suggestions.Count)
        {
            return;
        }

        var chosen = Suggestions[idx];
        var (prefix, _, _) = ComputeCompletion(TerminalInput);
        SetInputSilently(prefix + chosen + " ");
    }

    /// <summary>
    /// Complétion « shell » à la touche Tab : complète jusqu'au préfixe commun ; si
    /// plusieurs candidats sans progrès, les liste dans le terminal.
    /// </summary>
    public void CompleteTerminal()
    {
        var (prefix, word, matches) = ComputeCompletion(TerminalInput);
        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            SetInputSilently(prefix + matches[0] + " ");
            return;
        }

        var common = BrewCommandLine.CommonPrefix(matches);
        if (common.Length > word.Length)
        {
            _suppressSuggestions = true;
            TerminalInput = prefix + common;
            _suppressSuggestions = false;
            UpdateSuggestions();   // ré-ouvre le popup sur le nouveau préfixe
            return;
        }

        IsTerminalExpanded = true;
        CurrentScreen?.OutputLog.Add(string.Join("   ", matches.Take(80))
            + (matches.Count > 80 ? $"   … (+{matches.Count - 80})" : string.Empty));
    }

    partial void OnSelectedNavChanged(NavItem? value)
    {
        CurrentScreen = value?.Screen;
        if (value?.Screen is { } screen)
        {
            _ = screen.ActivateAsync();
        }
    }

    /// <summary>Sélectionne un écran par sa clé de titre (raccourcis clavier, menu de la barre).</summary>
    public void SelectScreen(string titleKey)
    {
        var item = NavItems.FirstOrDefault(n => n.TitleKey == titleKey);
        if (item is not null)
        {
            SelectedNav = item;
        }
    }

    /// <summary>Sélectionne le n-ième onglet (0-based) — raccourcis ⌘1…⌘8.</summary>
    public void SelectByIndex(int index)
    {
        if (index >= 0 && index < NavItems.Count)
        {
            SelectedNav = NavItems[index];
        }
    }
}
