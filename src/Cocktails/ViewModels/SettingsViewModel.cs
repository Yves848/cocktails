using Cocktails.Core;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Réglages » (P2) : surveillance des mises à jour, notifications, chemin de
/// brew, thème. Aperçu statique pour l'instant.
/// </summary>
public sealed class SettingsViewModel : ScreenViewModel
{
    public SettingsViewModel(IHomebrewService homebrew) : base(homebrew)
    {
        StatusMessage = "Réglages — aperçu.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SettingsViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Réglages";

    /// <summary>Chemin de l'exécutable brew (Apple Silicon par défaut).</summary>
    public string BrewPath => "/opt/homebrew/bin/brew";
}
