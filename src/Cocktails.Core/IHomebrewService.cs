using Cocktails.Core.Models;

namespace Cocktails.Core;

/// <summary>
/// Couche d'accès à Homebrew. Isole entièrement le reste de l'application de la
/// CLI <c>brew</c> : la couche UI (Avalonia/MVVM) ne dépend que de cette interface,
/// jamais de <see cref="System.Diagnostics.Process"/> ni du format de sortie de brew.
/// </summary>
public interface IHomebrewService
{
    /// <summary>Liste les packages actuellement installés (formulae et casks).</summary>
    Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>Recherche des packages par nom / mot-clé.</summary>
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Liste les packages installés pour lesquels une mise à jour est disponible.</summary>
    Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default);

    /// <summary>Récupère le détail enrichi d'un package (description, dépendances, homepage…).</summary>
    Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Installe un package.</summary>
    Task InstallAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Désinstalle un package installé.</summary>
    Task UninstallAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour un package précis, ou tous les packages obsolètes si
    /// <paramref name="name"/> est <c>null</c>.
    /// </summary>
    Task UpgradeAsync(string? name = null, CancellationToken cancellationToken = default);
}
