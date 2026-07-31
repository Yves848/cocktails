using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
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

    /// <summary>Formules installées auxquelles il manque des dépendances (<c>brew missing</c>).</summary>
    public ObservableCollection<MissingDependency> Missing { get; } = [];

    /// <summary>Vrai une fois la vérification effectuée (pilote l'affichage du résultat).</summary>
    [ObservableProperty]
    public partial bool MissingChecked { get; set; }

    /// <summary>Résumé lisible du résultat de la vérification.</summary>
    [ObservableProperty]
    public partial string MissingResult { get; set; } = string.Empty;

    /// <summary>Vrai si aucune dépendance ne manque (après vérification).</summary>
    public bool MissingIsHealthy => MissingChecked && Missing.Count == 0;

    partial void OnMissingCheckedChanged(bool value) => OnPropertyChanged(nameof(MissingIsHealthy));

    [RelayCommand]
    private Task CheckMissing() => RunAsync("Vérification des dépendances manquantes…", async () =>
    {
        var missing = await Homebrew.GetMissingAsync();
        Missing.Clear();
        foreach (var m in missing)
        {
            Missing.Add(m);
        }

        MissingChecked = true;
        MissingResult = missing.Count == 0
            ? "Aucune dépendance manquante — tout est complet."
            : $"{missing.Count} formule(s) avec des dépendances manquantes.";
        StatusMessage = MissingResult;
    });

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
