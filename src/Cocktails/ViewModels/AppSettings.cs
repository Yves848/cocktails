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

    /// <summary>Surveiller périodiquement les mises à jour en arrière-plan.</summary>
    [ObservableProperty]
    public partial bool MonitoringEnabled { get; set; } = true;

    /// <summary>Notifier via le centre de notifications quand de nouvelles maj arrivent.</summary>
    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    /// <summary>Intervalle entre deux vérifications automatiques (minutes).</summary>
    [ObservableProperty]
    public partial int MonitoringIntervalMinutes { get; set; } = 360;

    /// <summary>Chemin de l'exécutable brew (affiché en lecture seule).</summary>
    public string BrewPath { get; init; } = "/opt/homebrew/bin/brew";

    // Géométrie de la fenêtre (restaurée au démarrage). Null = valeurs par défaut.
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public bool WindowMaximized { get; set; }
}
