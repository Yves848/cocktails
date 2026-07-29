using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>Écran « Mises à jour » : packages obsolètes, mise à jour unitaire ou globale.</summary>
public partial class OutdatedViewModel : PackageListViewModel
{
    public OutdatedViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public OutdatedViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Mises à jour";

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Recherche des mises à jour…", async () =>
    {
        var outdated = await Homebrew.GetOutdatedAsync();
        Replace(outdated);
        StatusMessage = outdated.Count == 0
            ? "Tout est à jour."
            : $"{outdated.Count} mise(s) à jour disponible(s).";
    });

    [RelayCommand]
    private Task UpgradeAsync(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunAsync($"Mise à jour de « {package.Name} »…", async () =>
        {
            await Homebrew.UpgradeAsync(package.Name);
            await ReloadAsync();
            StatusMessage = $"« {package.Name} » à jour.";
        });
    }

    [RelayCommand]
    private Task UpgradeAllAsync() => RunAsync("Mise à jour de tous les packages…", async () =>
    {
        await Homebrew.UpgradeAsync();
        await ReloadAsync();
        StatusMessage = "Tous les packages sont à jour.";
    });

    private async Task ReloadAsync() => Replace(await Homebrew.GetOutdatedAsync());
}
