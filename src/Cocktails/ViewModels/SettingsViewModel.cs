using Cocktails.Core;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran « Réglages ». Le toggle « confirmer avant désinstallation » est fonctionnel
/// (partagé avec l'écran Installés). Surveillance et notifications viendront (P2).
/// </summary>
public sealed class SettingsViewModel : ScreenViewModel
{
    public SettingsViewModel(IHomebrewService homebrew, AppSettings? settings = null) : base(homebrew)
    {
        Settings = settings ?? new AppSettings();
        StatusMessage = "Réglages.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SettingsViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Réglages";

    public AppSettings Settings { get; }
}
