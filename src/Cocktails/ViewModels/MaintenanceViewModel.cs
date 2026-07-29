using Cocktails.Core;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Maintenance » (P2) : nettoyage, dépendances orphelines, diagnostic,
/// services. Placeholder pour l'instant — les opérations correspondantes ne sont
/// pas encore exposées par <see cref="IHomebrewService"/>.
/// </summary>
public sealed class MaintenanceViewModel : ScreenViewModel
{
    public MaintenanceViewModel(IHomebrewService homebrew) : base(homebrew)
    {
        StatusMessage = "Nettoyage & diagnostic — bientôt disponible.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public MaintenanceViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Maintenance";
}
