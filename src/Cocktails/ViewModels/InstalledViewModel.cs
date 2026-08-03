using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Installés » : liste des packages installés (master) + volet de détail
/// (brew info) et désinstallation.
/// </summary>
public partial class InstalledViewModel : PackageListViewModel
{
    private readonly AppSettings _settings;

    public InstalledViewModel(IHomebrewService homebrew, AppSettings? settings = null) : base(homebrew)
    {
        _settings = settings ?? new AppSettings();
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public InstalledViewModel() : this(new DesignHomebrewService())
    {
    }

    private System.Collections.Generic.HashSet<string> _leaves = new(System.StringComparer.Ordinal);

    protected override string TitleKey => "Nav.Installed";

    /// <summary>N'afficher que les paquets « racines » (installés explicitement, brew leaves).</summary>
    [ObservableProperty]
    public partial bool LeavesOnly { get; set; }

    /// <summary>Bascule le filtre « racines » (bouton on/off + raccourci ⌥R).</summary>
    [RelayCommand]
    private void ToggleLeaves() => LeavesOnly = !LeavesOnly;

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync(L["Status.LoadingInstalled"], async () =>
    {
        var installed = await Homebrew.GetInstalledAsync();
        _leaves = new System.Collections.Generic.HashSet<string>(
            await Homebrew.GetLeavesAsync(), System.StringComparer.Ordinal);
        Replace(installed);
        StatusMessage = L.Format("Status.InstalledCount", installed.Count);
    });

    partial void OnLeavesOnlyChanged(bool value)
    {
        ExtraFilter = value ? p => _leaves.Contains(p.Name) : null;
        ApplyFilter();
    }

    protected override Task<System.Collections.Generic.IReadOnlyList<string>> LoadDependentsAsync(Package package)
        => Homebrew.GetDependentsAsync(package.Name);

    /// <summary>Demande confirmation ; la désinstallation réelle a lieu au « Confirmer ».</summary>
    [RelayCommand]
    private void Uninstall(Package? package)
    {
        if (package is null)
        {
            return;
        }

        if (!_settings.ConfirmBeforeUninstall)
        {
            _ = DoUninstallAsync(package);
            return;
        }

        RequestConfirmation(
            L["Confirm.UninstallTitle"],
            L.Format("Confirm.UninstallMsg", package.Name),
            L["Confirm.UninstallBtn"],
            () => DoUninstallAsync(package));
    }

    [RelayCommand]
    private Task Reinstall(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.Reinstalling", package.Name), async progress =>
        {
            await Homebrew.ReinstallAsync(package.Name, progress);
            Replace(await Homebrew.GetInstalledAsync());
            StatusMessage = L.Format("Status.Reinstalled", package.Name);
        });
    }

    [RelayCommand]
    private Task Pin(Package? package) => TogglePinAsync(package, pin: true);

    [RelayCommand]
    private Task Unpin(Package? package) => TogglePinAsync(package, pin: false);

    private Task TogglePinAsync(Package? package, bool pin)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        var busy = pin ? "Status.Pinning" : "Status.Unpinning";
        return RunAsync(L.Format(busy, package.Name), async () =>
        {
            if (pin)
            {
                await Homebrew.PinAsync(package.Name);
            }
            else
            {
                await Homebrew.UnpinAsync(package.Name);
            }

            // Recharge le détail pour refléter le nouvel état épinglé.
            Details = await Homebrew.GetInfoAsync(package.Name);
            StatusMessage = L.Format(pin ? "Status.Pinned" : "Status.Unpinned", package.Name);
        });
    }

    private Task DoUninstallAsync(Package package)
        => RunWithOutputAsync(L.Format("Status.Uninstalling", package.Name), async progress =>
        {
            await Homebrew.UninstallAsync(package.Name, progress);
            ClearSelection();
            var installed = await Homebrew.GetInstalledAsync();
            Replace(installed);
            StatusMessage = L.Format("Status.Uninstalled", package.Name);
        });

    /// <summary>Désinstalle en une passe toutes les lignes cochées (confirmation d'abord).</summary>
    [RelayCommand]
    private void BatchUninstall()
    {
        var targets = CheckedPackages();
        if (targets.Count == 0)
        {
            return;
        }

        if (!_settings.ConfirmBeforeUninstall)
        {
            _ = DoBatchUninstallAsync(targets);
            return;
        }

        var names = string.Join(", ", targets.Select(t => t.Name));
        RequestConfirmation(
            L.Format("Confirm.BatchUninstallTitle", targets.Count),
            L.Format("Confirm.BatchUninstallMsg", names),
            L["Confirm.BatchUninstallBtn"],
            () => DoBatchUninstallAsync(targets));
    }

    /// <summary>Réinstalle en une passe toutes les lignes cochées (non destructif, sans confirmation).</summary>
    [RelayCommand]
    private Task BatchReinstall()
    {
        var targets = CheckedPackages();
        if (targets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.BatchReinstalling", targets.Count), async progress =>
        {
            var done = 0;
            foreach (var package in targets)
            {
                progress.Report($"$ brew reinstall {package.Name}");
                await Homebrew.ReinstallAsync(package.Name, progress);
                done++;
                StatusMessage = L.Format("Status.BatchReinstallProgress", done, targets.Count);
            }

            ClearSelection();
            Replace(await Homebrew.GetInstalledAsync());
            StatusMessage = L.Format("Status.BatchReinstalled", targets.Count);
        });
    }

    private Task DoBatchUninstallAsync(IReadOnlyList<Package> targets)
        => RunWithOutputAsync(L.Format("Status.BatchUninstalling", targets.Count), async progress =>
        {
            var done = 0;
            foreach (var package in targets)
            {
                progress.Report($"$ brew uninstall {package.Name}");
                await Homebrew.UninstallAsync(package.Name, progress);
                done++;
                StatusMessage = L.Format("Status.BatchUninstallProgress", done, targets.Count);
            }

            ClearSelection();
            Replace(await Homebrew.GetInstalledAsync());
            StatusMessage = L.Format("Status.BatchUninstalled", targets.Count);
        });
}
