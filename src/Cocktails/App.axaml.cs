using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cocktails.Core;
using Cocktails.Core.Process;
using Cocktails.Services;
using Cocktails.ViewModels;
using Cocktails.Views;

namespace Cocktails;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var runner = new ProcessRunner();
            var homebrew = new HomebrewService(runner);

            // Réglages : chargés du disque, ré-enregistrés à chaque changement.
            var settingsStore = new SettingsStore();
            var settings = settingsStore.Load();
            settings.PropertyChanged += (_, _) => settingsStore.Save(settings);

            // Monitoring des mises à jour + notifications système (natives en bundle .app,
            // osascript en dev — cf. PlatformNotifier).
            var notifier = PlatformNotifier.Create(runner);
            var monitor = new UpdateMonitor(homebrew, settings, notifier);

            var window = new MainWindow
            {
                DataContext = new MainViewModel(homebrew, settings, monitor),
            };

            RestoreWindowGeometry(window, settings);
            window.Closing += (_, _) => SaveWindowGeometry(window, settings, settingsStore);

            desktop.MainWindow = window;

            monitor.Start();
        }

        base.OnFrameworkInitializationCompleted();
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