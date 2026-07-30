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
    // Séquence pour ignorer les résultats de détail obsolètes (sélection rapide).
    private int _detailToken;

    public InstalledViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public InstalledViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Installés";

    /// <summary>Package sélectionné dans la liste (pilote le volet de détail).</summary>
    [ObservableProperty]
    public partial Package? SelectedPackage { get; set; }

    /// <summary>Détail du package sélectionné, ou <c>null</c> si aucun.</summary>
    [ObservableProperty]
    public partial PackageDetails? Details { get; set; }

    /// <summary>Chargement du détail en cours (spinner local, sans overlay global).</summary>
    [ObservableProperty]
    public partial bool IsLoadingDetails { get; set; }

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Chargement des packages installés…", async () =>
    {
        var installed = await Homebrew.GetInstalledAsync();
        Replace(installed);
        StatusMessage = $"{installed.Count} package(s) installé(s).";
    });

    partial void OnSelectedPackageChanged(Package? value) => _ = LoadDetailsAsync(value);

    private async Task LoadDetailsAsync(Package? package)
    {
        var token = ++_detailToken;
        if (package is null)
        {
            Details = null;
            return;
        }

        IsLoadingDetails = true;
        try
        {
            var details = await Homebrew.GetInfoAsync(package.Name);
            if (token == _detailToken)
            {
                Details = details;
            }
        }
        catch (System.Exception)
        {
            if (token == _detailToken)
            {
                Details = null;
            }
        }
        finally
        {
            if (token == _detailToken)
            {
                IsLoadingDetails = false;
            }
        }
    }

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
            SelectedPackage = null;
            Details = null;
            var installed = await Homebrew.GetInstalledAsync();
            Replace(installed);
            StatusMessage = $"« {package.Name} » désinstallé.";
        });
    }
}
