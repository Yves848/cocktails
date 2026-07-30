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
}
