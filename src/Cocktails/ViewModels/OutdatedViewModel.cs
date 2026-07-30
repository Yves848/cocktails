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

        return RunWithOutputAsync($"Mise à jour de « {package.Name} »…", async progress =>
        {
            await Homebrew.UpgradeAsync(package.Name, progress);
            await ReloadAsync();
            StatusMessage = $"« {package.Name} » à jour.";
        });
    }

    [RelayCommand]
    private Task UpgradeAllAsync() => RunWithOutputAsync("Mise à jour de tous les packages…", async progress =>
    {
        await Homebrew.UpgradeAsync(null, progress);
        await ReloadAsync();
        StatusMessage = "Tous les packages sont à jour.";
    });

    private async Task ReloadAsync() => Replace(await Homebrew.GetOutdatedAsync());
}
