namespace Cocktails.Core.Models;

/// <summary>
/// Détail enrichi d'un package, obtenu via <c>brew info --json=v2 &lt;name&gt;</c>.
/// Alimente l'écran de détail (description, dépendances, homepage, versions…).
/// </summary>
public record PackageDetails(
    string Name,
    PackageKind Kind,
    string? Description,
    string? Homepage,
    string? StableVersion,
    string? InstalledVersion,
    IReadOnlyList<string> Dependencies,
    bool IsPinned,
    string? Tap)
{
    /// <summary>Vrai si une version est installée localement.</summary>
    public bool IsInstalled => InstalledVersion is not null;

    /// <summary>Vrai si une mise à jour est disponible.</summary>
    public bool IsOutdated =>
        IsInstalled && StableVersion is not null && InstalledVersion != StableVersion;

    /// <summary>Libellé lisible du type (« Formula » / « Cask »).</summary>
    public string KindLabel => Kind == PackageKind.Cask ? "Cask" : "Formula";

    /// <summary>Initiale du type pour le badge (« F » / « C »).</summary>
    public string KindBadge => Kind == PackageKind.Cask ? "C" : "F";

    /// <summary>Vrai s'il y a au moins une dépendance à afficher.</summary>
    public bool HasDependencies => Dependencies.Count > 0;
}
