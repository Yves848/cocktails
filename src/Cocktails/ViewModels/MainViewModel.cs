using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Cocktails.Core;
using Cocktails.Services;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public MainViewModel(IHomebrewService homebrew, AppSettings? settings = null, UpdateMonitor? monitor = null)
    {
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
            new NavItem("Nav.Settings", IconSettings, new SettingsViewModel(homebrew, settings), "#94A3B8"),
            new NavItem("Nav.Help", IconHelp, new HelpViewModel(), "#FB923C"),
        ];

        // Le badge « Mises à jour » suit le compteur du moniteur.
        Monitor = monitor ?? new UpdateMonitor(homebrew, settings, new NullNotifier());
        Monitor.PropertyChanged += OnMonitorChanged;
        Monitor.OutdatedChanged += OnOutdatedChanged;
        _updatesNav.Count = Monitor.OutdatedCount;

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
