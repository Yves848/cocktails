using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>Écran « Services » : liste et pilotage des services Homebrew (brew services).</summary>
public partial class ServicesViewModel : ScreenViewModel
{
    public ServicesViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public ServicesViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Services";

    public ObservableCollection<BrewService> Services { get; } = [];

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Chargement des services…", async () =>
    {
        var services = await Homebrew.GetServicesAsync();
        Services.Clear();
        foreach (var s in services)
        {
            Services.Add(s);
        }

        StatusMessage = $"{services.Count} service(s).";
    });

    [RelayCommand]
    private Task Start(BrewService? service)
        => Act(service, "Démarrage", (name, p) => Homebrew.StartServiceAsync(name, p));

    [RelayCommand]
    private Task Stop(BrewService? service)
        => Act(service, "Arrêt", (name, p) => Homebrew.StopServiceAsync(name, p));

    [RelayCommand]
    private Task Restart(BrewService? service)
        => Act(service, "Redémarrage", (name, p) => Homebrew.RestartServiceAsync(name, p));

    private Task Act(BrewService? service, string verb, Func<string, IProgress<string>, Task> operation)
    {
        if (service is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync($"{verb} de « {service.Name} »…", async progress =>
        {
            await operation(service.Name, progress);
            var services = await Homebrew.GetServicesAsync();
            Services.Clear();
            foreach (var s in services)
            {
                Services.Add(s);
            }

            StatusMessage = $"« {service.Name} » — {verb.ToLowerInvariant()} effectué.";
        });
    }
}
