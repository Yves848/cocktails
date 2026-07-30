using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Installés » : liste des packages installés (master) + volet de détail
/// (brew info) et désinstallation.
/// </summary>
public partial class InstalledViewModel : PackageListViewModel
{
    public InstalledViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public InstalledViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Installés";

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Chargement des packages installés…", async () =>
    {
        var installed = await Homebrew.GetInstalledAsync();
        Replace(installed);
        StatusMessage = $"{installed.Count} package(s) installé(s).";
    });

    [RelayCommand]
    private Task UninstallAsync(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunAsync($"Désinstallation de « {package.Name} »…", async () =>
        {
            await Homebrew.UninstallAsync(package.Name);
            ClearSelection();
            var installed = await Homebrew.GetInstalledAsync();
            Replace(installed);
            StatusMessage = $"« {package.Name} » désinstallé.";
        });
    }
}
