using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Cocktails.Core;
using Cocktails.Core.Process;
using Cocktails.Core.Sudo;
using Cocktails.Localization;
using Cocktails.Services;
using Cocktails.ViewModels;
using Cocktails.Views;

namespace Cocktails;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _window;
    private AppSettings? _settings;
    private SettingsStore? _settingsStore;
    private UpdateMonitor? _monitor;
    private AskpassBroker _askpass = null!;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _openItem;
    private NativeMenuItem? _updatesItem;
    private NativeMenuItem? _searchItem;
    private NativeMenuItem? _checkNowItem;
    private NativeMenuItem? _quitItem;
    private bool _quitting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;

            // L'app vit dans la barre de menu : elle ne se ferme pas quand la fenêtre
            // est masquée ; seul « Quitter » (menu de l'icône / ⌘Q) l'arrête.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var runner = new ProcessRunner();

            // Mot de passe administrateur : sans terminal, sudo ne peut pas le demander.
            // Le courtier installe un programme askpass que brew utilisera (SUDO_ASKPASS),
            // et qui vient poser la question dans l'interface.
            _askpass = new AskpassBroker(SettingsStore.DirectoryPath);
            _ = _askpass.StartAsync();

            var homebrew = new HomebrewService(runner, askpass: _askpass);

            // Réglages : chargés du disque, ré-enregistrés à chaque changement.
            _settingsStore = new SettingsStore();
            _settings = _settingsStore.Load();
            _askpass.CacheLifetime = LifetimeOf(_settings.SudoPasswordLifetimeMinutes);

            // Langue : appliquée avant de construire l'UI, puis suivie à chaud.
            Localizer.Instance.SetLanguage(_settings.Language);
            Localizer.Instance.LanguageChanged += (_, _) => UpdateTrayLabels();
            _settings.PropertyChanged += (_, e) =>
            {
                _settingsStore.Save(_settings);
                if (e.PropertyName == nameof(AppSettings.Language))
                {
                    Localizer.Instance.SetLanguage(_settings.Language);
                }
                else if (e.PropertyName == nameof(AppSettings.SudoPasswordLifetimeMinutes))
                {
                    _askpass.CacheLifetime = LifetimeOf(_settings.SudoPasswordLifetimeMinutes);
                }
            };

            // Monitoring des mises à jour + notifications système (natives en bundle .app,
            // osascript en dev — cf. PlatformNotifier).
            var notifier = PlatformNotifier.Create(runner);
            _monitor = new UpdateMonitor(homebrew, _settings, notifier);

            var shell = new MainViewModel(homebrew, _settings, _monitor, notifier, _askpass);
            _window = new MainWindow { DataContext = shell };
            _window.Opened += (_, _) => EnsureVisibleOnScreen(_window!);

            // brew réclame le mot de passe depuis un thread de fond : on repasse sur le
            // thread UI, et on démasque la fenêtre — l'app peut être réduite à son icône
            // de barre de menu au moment où l'installeur le demande.
            _askpass.Handler = (prompt, cancellationToken) =>
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowWindow();
                    return shell.RequestPasswordAsync(prompt, cancellationToken);
                });

            // Options de lancement (captures d'écran) : un lancement = un écran, plutôt
            // que de piloter l'interface au clavier — peu fiable pour une app agent.
            ApplyStartupOptions(shell, StartupOptions.Parse(Environment.GetCommandLineArgs()));

            RestoreWindowGeometry(_window, _settings);
            _window.Closing += OnWindowClosing;

            desktop.MainWindow = _window;

            BuildTrayIcon();
            _monitor.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(UpdateMonitor.OutdatedCount))
                {
                    UpdateTrayUpdatesLabel();
                }
            };

            _monitor.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Applique les options de ligne de commande. La sélection d'un paquet attend que
    /// l'écran ait fini son chargement initial (la liste est vide avant).
    /// </summary>
    private static void ApplyStartupOptions(MainViewModel shell, StartupOptions options)
    {
        if (options.ScreenKey is not { } key)
        {
            return;
        }

        shell.SelectScreen(key);

        if (options.SelectPackage is { } name && shell.CurrentScreen is PackageListViewModel list)
        {
            Dispatcher.UIThread.Post(
                async () =>
                {
                    await list.ActivateAsync();
                    list.SelectByName(name);
                },
                DispatcherPriority.Background);
        }
    }

    /// <summary>Durée de rétention réglée ; <see cref="int.MaxValue"/> = toute la session.</summary>
    private static TimeSpan LifetimeOf(int minutes) =>
        minutes == int.MaxValue ? Timeout.InfiniteTimeSpan : TimeSpan.FromMinutes(minutes);

    // --- Barre de menu (icône + menu popup) ----------------------------------

    private void BuildTrayIcon()
    {
        var menu = new NativeMenu();

        _openItem = new NativeMenuItem();
        _openItem.Click += (_, _) => ShowWindow();

        _updatesItem = new NativeMenuItem();
        _updatesItem.Click += (_, _) => ShowScreen("Nav.Updates");

        _searchItem = new NativeMenuItem();
        _searchItem.Click += (_, _) => ShowScreen("Nav.Search");

        _checkNowItem = new NativeMenuItem();
        _checkNowItem.Click += (_, _) => _ = _monitor?.CheckNowAsync();

        _quitItem = new NativeMenuItem();
        _quitItem.Click += (_, _) => Quit();

        menu.Items.Add(_openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_updatesItem);
        menu.Items.Add(_searchItem);
        menu.Items.Add(_checkNowItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_quitItem);

        UpdateTrayLabels();

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Cocktails/Assets/tray.png"))),
            ToolTipText = "Cocktails",
            IsVisible = true,
            Menu = menu,
        };
        _trayIcon.Clicked += (_, _) => ShowWindow();
    }

    /// <summary>(Ré)applique les libellés traduits du menu de la barre de menu.</summary>
    private void UpdateTrayLabels()
    {
        var l = Localizer.Instance;
        if (_openItem is not null)
        {
            _openItem.Header = l["Tray.Open"];
        }

        if (_searchItem is not null)
        {
            _searchItem.Header = l["Tray.Search"];
        }

        if (_checkNowItem is not null)
        {
            _checkNowItem.Header = l["Tray.CheckNow"];
        }

        if (_quitItem is not null)
        {
            _quitItem.Header = l["Tray.Quit"];
        }

        UpdateTrayUpdatesLabel();
    }

    private void UpdateTrayUpdatesLabel()
    {
        if (_updatesItem is null)
        {
            return;
        }

        var count = _monitor?.OutdatedCount ?? 0;
        _updatesItem.Header = count > 0
            ? Localizer.Instance.Format("Tray.UpdatesCount", count)
            : Localizer.Instance["Nav.Updates"];
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    private void ShowScreen(string title)
    {
        ShowWindow();
        (_window?.DataContext as MainViewModel)?.SelectScreen(title);
    }

    // --- Fermeture / arrière-plan / quitter ----------------------------------

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_window is not null && _settings is not null && _settingsStore is not null)
        {
            SaveWindowGeometry(_window, _settings, _settingsStore);
        }

        // Rester en arrière-plan : on annule la fermeture et on masque la fenêtre.
        if (!_quitting && _settings?.KeepRunningInBackground == true)
        {
            e.Cancel = true;
            _window?.Hide();
            return;
        }

        // Sinon (réglage désactivé ou quitter explicite) : on arrête réellement l'app.
        if (!_quitting)
        {
            _quitting = true;
            Dispatcher.UIThread.Post(() => _desktop?.Shutdown());
        }
    }

    /// <summary>Quitte réellement l'application (menu de l'icône, ⌘Q).</summary>
    public void Quit()
    {
        if (_quitting)
        {
            return;
        }

        _quitting = true;
        if (_window is not null && _settings is not null && _settingsStore is not null)
        {
            SaveWindowGeometry(_window, _settings, _settingsStore);
        }

        // Ferme la socket askpass, retire son fichier et efface le mot de passe éventuellement
        // gardé en mémoire.
        _askpass?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));

        _trayIcon?.Dispose();
        _desktop?.Shutdown();
    }

    /// <summary>
    /// Garantit que la fenêtre restaurée est visible : si sa position tombe hors de tout
    /// écran actuel (ex. géométrie mémorisée sur un moniteur externe désormais absent),
    /// on la recentre sur l'écran principal. Appelée après <c>Opened</c> (les écrans ne
    /// sont connus qu'une fois la fenêtre créée côté plateforme).
    /// </summary>
    private static void EnsureVisibleOnScreen(Window window)
    {
        var screens = window.Screens;
        if (screens is null || screens.All.Count == 0)
        {
            return;
        }

        var pos = window.Position;

        // Visible si le coin haut-gauche est dans un écran (marge de sécurité incluse
        // via WorkingArea, qui exclut menu-bar/dock).
        var onScreen = false;
        foreach (var s in screens.All)
        {
            if (s.Bounds.Contains(pos))
            {
                onScreen = true;
                break;
            }
        }

        if (onScreen)
        {
            return;
        }

        var target = screens.Primary ?? screens.All[0];
        var wa = target.WorkingArea;
        var scale = target.Scaling;
        var pw = (int)(window.Bounds.Width * scale);
        var ph = (int)(window.Bounds.Height * scale);
        var nx = wa.X + Math.Max(0, (wa.Width - pw) / 2);
        var ny = wa.Y + Math.Max(0, (wa.Height - ph) / 2);
        window.Position = new PixelPoint(nx, ny);
    }

    private static void RestoreWindowGeometry(Window window, AppSettings settings)
    {
        if (settings.WindowWidth is { } w && settings.WindowHeight is { } h)
        {
            window.Width = w;
            window.Height = h;
        }

        if (settings.WindowX is { } x && settings.WindowY is { } y)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint(x, y);
        }

        if (settings.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    private static void SaveWindowGeometry(Window window, AppSettings settings, SettingsStore store)
    {
        settings.WindowMaximized = window.WindowState == WindowState.Maximized;

        // Ne mémoriser taille/position qu'en état normal (max/min donnent des bornes atypiques).
        if (window.WindowState == WindowState.Normal)
        {
            settings.WindowWidth = window.Bounds.Width;
            settings.WindowHeight = window.Bounds.Height;
            settings.WindowX = window.Position.X;
            settings.WindowY = window.Position.Y;
        }

        store.Save(settings);
    }
}
