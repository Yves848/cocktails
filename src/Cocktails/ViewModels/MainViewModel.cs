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
    // Noms formulae+casks pour la complétion (chargés paresseusement à l'ouverture).
    private IReadOnlyList<string>? _names;
    private bool _namesLoading;

    public MainViewModel(IHomebrewService homebrew, AppSettings? settings = null, UpdateMonitor? monitor = null,
        INotifier? notifier = null)
    {
        _homebrew = homebrew;
        settings ??= new AppSettings();
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

        var args = await screen.RunTerminalCommandAsync(input);
        if (args is not null && BrewCommandLine.IsMutating(args))
        {
            screen.Invalidate();
            await screen.ActivateAsync();
        }
    }

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
            _ = LoadNamesAsync();
        }
    }

    private async Task LoadNamesAsync()
    {
        if (_names is not null || _namesLoading)
        {
            return;
        }

        _namesLoading = true;
        try
        {
            _names = await _homebrew.GetAllNamesAsync();
        }
        catch (Exception)
        {
            _names = [];
        }
        finally
        {
            _namesLoading = false;
        }
    }

    /// <summary>
    /// Complète le mot courant de la saisie (Tab) : sous-commandes brew pour le premier
    /// mot, sinon noms de paquets. Complète jusqu'au préfixe commun ; si aucun progrès et
    /// plusieurs candidats, les liste dans le terminal (comportement type shell).
    /// </summary>
    public void CompleteTerminal()
    {
        var text = TerminalInput;
        var lastSpace = text.LastIndexOf(' ');
        var prefixPart = lastSpace < 0 ? string.Empty : text[..(lastSpace + 1)];
        var word = lastSpace < 0 ? text : text[(lastSpace + 1)..];

        var before = (lastSpace < 0 ? string.Empty : text[..lastSpace]).Trim();
        var firstWord = before.Length == 0 || before.Equals("brew", StringComparison.OrdinalIgnoreCase);

        IReadOnlyList<string> pool;
        if (firstWord)
        {
            pool = BrewCommandLine.Subcommands;
        }
        else if (_names is { } names)
        {
            pool = names;
        }
        else
        {
            _ = LoadNamesAsync();   // pas encore chargés : on lance et on abandonne ce Tab
            return;
        }

        var matches = pool
            .Where(n => n.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            TerminalInput = prefixPart + matches[0] + " ";
            return;
        }

        var common = BrewCommandLine.CommonPrefix(matches);
        if (common.Length > word.Length)
        {
            TerminalInput = prefixPart + common;
            return;
        }

        // Aucun progrès possible : on affiche les candidats dans le terminal.
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
