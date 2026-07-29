namespace Cocktails.Core.Models;

/// <summary>
/// Un package Homebrew (formula ou cask), tel qu'exposé à l'UI.
/// </summary>
/// <param name="Name">Nom du package (ex. <c>git</c>, <c>visual-studio-code</c>).</param>
/// <param name="Kind">Formula ou Cask.</param>
/// <param name="InstalledVersion">Version installée, ou <c>null</c> si non installé.</param>
/// <param name="LatestVersion">Dernière version connue côté Homebrew, si disponible.</param>
/// <param name="Description">Description courte fournie par Homebrew, si disponible.</param>
public record Package(
    string Name,
    PackageKind Kind,
    string? InstalledVersion = null,
    string? LatestVersion = null,
    string? Description = null)
{
    /// <summary>Vrai si une version est installée localement.</summary>
    public bool IsInstalled => InstalledVersion is not null;

    /// <summary>
    /// Vrai si une mise à jour est disponible (version installée différente de la dernière).
    /// </summary>
    public bool IsOutdated =>
        IsInstalled && LatestVersion is not null && InstalledVersion != LatestVersion;

    /// <summary>Libellé lisible du type (« Formula » / « Cask »).</summary>
    public string KindLabel => Kind == PackageKind.Cask ? "Cask" : "Formula";

    /// <summary>Initiale du type pour le badge de la liste (« F » / « C »).</summary>
    public string KindBadge => Kind == PackageKind.Cask ? "C" : "F";
}

/// <summary>Nature d'un package Homebrew.</summary>
public enum PackageKind
{
    Formula,
    Cask,
}
