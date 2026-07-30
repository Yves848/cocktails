using System.Threading.Tasks;
using Cocktails.Core;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Maintenance » : nettoyage, dépendances orphelines, diagnostic. Chaque action
/// diffuse son log dans l'overlay (via <see cref="ScreenViewModel.RunWithOutputAsync"/>).
/// </summary>
public sealed partial class MaintenanceViewModel : ScreenViewModel
{
    public MaintenanceViewModel(IHomebrewService homebrew) : base(homebrew)
    {
        StatusMessage = "Nettoyage & diagnostic.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public MaintenanceViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Maintenance";

    [RelayCommand]
    private Task Cleanup()
        => RunWithOutputAsync("Nettoyage (brew cleanup)…", p => Homebrew.CleanupAsync(p));

    /// <summary>Retire les dépendances orphelines — confirmé car cela supprime des paquets.</summary>
    [RelayCommand]
    private void Autoremove()
        => RequestConfirmation(
            "Retirer les dépendances orphelines ?",
            "« brew autoremove » supprimera les formulae installées automatiquement et devenues inutiles.",
            "Retirer",
            () => RunWithOutputAsync("Suppression des dépendances orphelines…", p => Homebrew.AutoremoveAsync(p)));

    [RelayCommand]
    private Task Doctor()
        => RunWithOutputAsync("Diagnostic (brew doctor)…", p => Homebrew.DoctorAsync(p));

    /// <summary>Exporte l'installé vers <paramref name="path"/> (Brewfile). Appelé après le sélecteur.</summary>
    public Task ExportBrewfileAsync(string path)
        => RunWithOutputAsync("Export du Brewfile…", async progress =>
        {
            await Homebrew.BundleDumpAsync(path, progress);
            StatusMessage = "Brewfile exporté.";
        });

    /// <summary>Installe depuis <paramref name="path"/> (Brewfile) — confirmé (installe des paquets).</summary>
    public void ImportBrewfile(string path)
        => RequestConfirmation(
            "Importer ce Brewfile ?",
            "Les entrées manquantes (taps, formulae, casks) seront installées. Cela peut être long.",
            "Importer",
            () => RunWithOutputAsync("Import du Brewfile…", async progress =>
            {
                await Homebrew.BundleInstallAsync(path, progress);
                StatusMessage = "Brewfile importé.";
            }));
}
