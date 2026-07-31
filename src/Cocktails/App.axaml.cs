using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Cocktails.Core;
using Cocktails.Core.Process;
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
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _updatesItem;
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
            var homebrew = new HomebrewService(runner);

            // Réglages : chargés du disque, ré-enregistrés à chaque changement.
            _settingsStore = new SettingsStore();
            _settings = _settingsStore.Load();
            _settings.PropertyChanged += (_, _) => _settingsStore.Save(_settings);

            // Monitoring des mises à jour + notifications système (natives en bundle .app,
            // osascript en dev — cf. PlatformNotifier).
            var notifier = PlatformNotifier.Create(runner);
            _monitor = new UpdateMonitor(homebrew, _settings, notifier);

            _window = new MainWindow
            {
                DataContext = new MainViewModel(homebrew, _settings, _monitor),
            };

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

    // --- Barre de menu (icône + menu popup) ----------------------------------

    private void BuildTrayIcon()
    {
        var menu = new NativeMenu();

        var open = new NativeMenuItem("Ouvrir Cocktails");
        open.Click += (_, _) => ShowWindow();

        _updatesItem = new NativeMenuItem("Mises à jour");
        _updatesItem.Click += (_, _) => ShowScreen("Mises à jour");
        UpdateTrayUpdatesLabel();

        var search = new NativeMenuItem("Rechercher…");
        search.Click += (_, _) => ShowScreen("Rechercher");

        var checkNow = new NativeMenuItem("Vérifier maintenant");
        checkNow.Click += (_, _) => _ = _monitor?.CheckNowAsync();

        var quit = new NativeMenuItem("Quitter Cocktails");
        quit.Click += (_, _) => Quit();

        menu.Items.Add(open);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_updatesItem);
        menu.Items.Add(search);
        menu.Items.Add(checkNow);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(quit);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Cocktails/Assets/tray.png"))),
            ToolTipText = "Cocktails",
            IsVisible = true,
            Menu = menu,
        };
        _trayIcon.Clicked += (_, _) => ShowWindow();
    }

    private void UpdateTrayUpdatesLabel()
    {
        if (_updatesItem is null)
        {
            return;
        }

        var count = _monitor?.OutdatedCount ?? 0;
        _updatesItem.Header = count > 0 ? $"Mises à jour ({count})" : "Mises à jour";
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

        _trayIcon?.Dispose();
        _desktop?.Shutdown();
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
