using Avalonia;
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
            var homebrew = new HomebrewService(new ProcessRunner());

            // Réglages : chargés du disque, ré-enregistrés à chaque changement.
            var settingsStore = new SettingsStore();
            var settings = settingsStore.Load();
            settings.PropertyChanged += (_, _) => settingsStore.Save(settings);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(homebrew, settings),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}