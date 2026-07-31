using System.Collections.Generic;
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

    protected override string TitleKey => "Nav.Updates";

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Actualise l'index Homebrew (brew update) puis recharge les obsolètes.</summary>
    [RelayCommand]
    private Task UpdateIndexAsync() => RunWithOutputAsync(L["Status.IndexUpdating"], async progress =>
    {
        await Homebrew.UpdateIndexAsync(progress);
        await ReloadAsync();
        StatusMessage = Packages.Count == 0
            ? L["Status.IndexUpToDate"]
            : L.Format("Status.IndexUpdatesAvailable", Packages.Count);
    });

    private Task LoadAsync() => RunAsync(L["Status.LoadingUpdates"], async () =>
    {
        var outdated = await Homebrew.GetOutdatedAsync();
        Replace(outdated);
        StatusMessage = outdated.Count == 0
            ? L["Status.AllUpToDate"]
            : L.Format("Status.UpdatesAvailable", outdated.Count);
    });

    [RelayCommand]
    private Task UpgradeAsync(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.Upgrading", package.Name), async progress =>
        {
            await Homebrew.UpgradeAsync(package.Name, progress);
            await ReloadAsync();
            StatusMessage = L.Format("Status.Upgraded", package.Name);
        });
    }

    [RelayCommand]
    private Task UpgradeAllAsync() => RunWithOutputAsync(L["Status.UpgradingAll"], async progress =>
    {
        await Homebrew.UpgradeAsync(null, progress);
        await ReloadAsync();
        StatusMessage = L["Status.AllUpgraded"];
    });

    /// <summary>Met à jour en une passe toutes les lignes cochées.</summary>
    [RelayCommand]
    private Task BatchUpgradeAsync()
    {
        var targets = CheckedPackages();
        if (targets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.BatchUpgrading", targets.Count), async progress =>
        {
            var done = 0;
            foreach (var package in targets)
            {
                progress.Report($"$ brew upgrade {package.Name}");
                await Homebrew.UpgradeAsync(package.Name, progress);
                done++;
                StatusMessage = L.Format("Status.BatchUpgradeProgress", done, targets.Count);
            }

            await ReloadAsync();
            StatusMessage = L.Format("Status.BatchUpgraded", targets.Count);
        });
    }

    private async Task ReloadAsync() => Replace(await Homebrew.GetOutdatedAsync());
}
