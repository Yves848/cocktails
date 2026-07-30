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

    /// <summary>Installe un package. <paramref name="output"/> reçoit le log brew en direct.</summary>
    Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Désinstalle un package installé. <paramref name="output"/> reçoit le log brew.</summary>
    Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour un package précis, ou tous les packages obsolètes si
    /// <paramref name="name"/> est <c>null</c>. <paramref name="output"/> reçoit le log brew.
    /// </summary>
    Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Nettoie le cache et les anciennes versions (<c>brew cleanup</c>).</summary>
    Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Retire les dépendances installées automatiquement et devenues inutiles (<c>brew autoremove</c>).</summary>
    Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);

    /// <summary>Diagnostic de l'installation (<c>brew doctor</c>). Ne lève pas sur avertissements.</summary>
    Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default);
}
