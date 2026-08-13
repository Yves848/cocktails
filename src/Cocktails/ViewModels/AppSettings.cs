using Cocktails.Localization;
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

    /// <summary>
    /// Rester actif en arrière-plan : fermer la fenêtre la masque (l'app continue de
    /// tourner, accessible via l'icône de la barre de menu). Sinon, fermer quitte l'app.
    /// </summary>
    [ObservableProperty]
    public partial bool KeepRunningInBackground { get; set; } = true;

    /// <summary>
    /// Durée (minutes) pendant laquelle un mot de passe administrateur « retenu » survit
    /// sans nouvel appel sudo, après quoi il est effacé de la mémoire. <see cref="int.MaxValue"/>
    /// = toute la session. Jamais écrit sur disque : seule la durée l'est.
    /// </summary>
    [ObservableProperty]
    public partial int SudoPasswordLifetimeMinutes { get; set; } = 60;

    /// <summary>Langue de l'interface (<see cref="AppLanguage.System"/> = suivre le système).</summary>
    [ObservableProperty]
    public partial AppLanguage Language { get; set; } = AppLanguage.System;

    /// <summary>
    /// Raccourci d'ouverture/focus du terminal, au format <see cref="Avalonia.Input.KeyGesture"/>
    /// (ex. « Cmd+T », « Ctrl+Alt+J »). Configurable dans les Réglages.
    /// </summary>
    [ObservableProperty]
    public partial string TerminalShortcut { get; set; } = "Cmd+T";

    /// <summary>Chemin de l'exécutable brew (affiché en lecture seule).</summary>
    public string BrewPath { get; init; } = "/opt/homebrew/bin/brew";

    // Géométrie de la fenêtre (restaurée au démarrage). Null = valeurs par défaut.
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public bool WindowMaximized { get; set; }
}
