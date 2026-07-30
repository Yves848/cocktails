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

    public override string Title => "Installés";

    /// <summary>N'afficher que les paquets « racines » (installés explicitement, brew leaves).</summary>
    [ObservableProperty]
    public partial bool LeavesOnly { get; set; }

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Chargement des packages installés…", async () =>
    {
        var installed = await Homebrew.GetInstalledAsync();
        _leaves = new System.Collections.Generic.HashSet<string>(
            await Homebrew.GetLeavesAsync(), System.StringComparer.Ordinal);
        Replace(installed);
        StatusMessage = $"{installed.Count} package(s) installé(s).";
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
            "Désinstaller ce paquet ?",
            $"La désinstallation de « {package.Name} » est irréversible. Les paquets qui en dépendent pourraient cesser de fonctionner.",
            "Désinstaller",
            () => DoUninstallAsync(package));
    }

    [RelayCommand]
    private Task Reinstall(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync($"Réinstallation de « {package.Name} »…", async progress =>
        {
            await Homebrew.ReinstallAsync(package.Name, progress);
            Replace(await Homebrew.GetInstalledAsync());
            StatusMessage = $"« {package.Name} » réinstallé.";
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

        var verb = pin ? "Épinglage" : "Désépinglage";
        return RunAsync($"{verb} de « {package.Name} »…", async () =>
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
            StatusMessage = pin ? $"« {package.Name} » épinglé." : $"« {package.Name} » désépinglé.";
        });
    }

    private Task DoUninstallAsync(Package package)
        => RunWithOutputAsync($"Désinstallation de « {package.Name} »…", async progress =>
        {
            await Homebrew.UninstallAsync(package.Name, progress);
            ClearSelection();
            var installed = await Homebrew.GetInstalledAsync();
            Replace(installed);
            StatusMessage = $"« {package.Name} » désinstallé.";
        });
}
