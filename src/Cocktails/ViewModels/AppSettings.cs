using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>
/// Réglages de l'application, partagés entre les écrans (une seule instance créée par
/// le shell). En mémoire pour l'instant — la persistance disque viendra plus tard.
/// </summary>
public partial class AppSettings : ObservableObject
{
    /// <summary>Demander confirmation avant une désinstallation.</summary>
    [ObservableProperty]
    public partial bool ConfirmBeforeUninstall { get; set; } = true;

    /// <summary>Chemin de l'exécutable brew (affiché en lecture seule).</summary>
    public string BrewPath { get; init; } = "/opt/homebrew/bin/brew";
}
