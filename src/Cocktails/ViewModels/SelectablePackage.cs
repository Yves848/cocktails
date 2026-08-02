using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>
/// Enveloppe un <see cref="Package"/> d'une liste avec un état « coché » observable,
/// pour les opérations par lots (mise à jour / désinstallation multiples). Les propriétés
/// de présentation sont relayées telles quelles vers le package sous-jacent, afin que les
/// gabarits de liste restent inchangés.
/// </summary>
public partial class SelectablePackage : ObservableObject
{
    public SelectablePackage(Package package) => Package = package;

    /// <summary>Package sous-jacent (immuable).</summary>
    public Package Package { get; }

    /// <summary>Ligne cochée pour une opération par lot.</summary>
    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    /// <summary>Tuile sélectionnée (pilote le liseré accent) ; maintenu par le VM.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    // Relais de présentation (évite de réécrire les gabarits en Package.Xxx).
    public string Name => Package.Name;
    public string KindLabel => Package.KindLabel;
    public string KindBadge => Package.KindBadge;
    public string? InstalledVersion => Package.InstalledVersion;
    public string? LatestVersion => Package.LatestVersion;
    public bool IsInstalled => Package.IsInstalled;
    public bool IsOutdated => Package.IsOutdated;
    public string? Homepage => Package.Homepage;
}
