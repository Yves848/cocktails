using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;

namespace Cocktails.ViewModels;

/// <summary>
/// Implémentation factice de <see cref="IHomebrewService"/> qui renvoie des données
/// d'exemple. Sert uniquement au previewer XAML (design-time) et au constructeur
/// sans argument de <see cref="MainViewModel"/> ; jamais utilisée à l'exécution réelle.
/// </summary>
internal sealed class DesignHomebrewService : IHomebrewService
{
    private static readonly IReadOnlyList<Package> Sample =
    [
        new("git", PackageKind.Formula, InstalledVersion: "2.45.2"),
        new("ripgrep", PackageKind.Formula, InstalledVersion: "14.1.0"),
        new("visual-studio-code", PackageKind.Cask, InstalledVersion: "1.90.0"),
    ];

    public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Sample);

    public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult(Sample);

    public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Package>>([Sample[0]]);

    public Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UnpinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(new PackageDetails(
            name, PackageKind.Formula,
            "Système de gestion de versions distribué.",
            "https://git-scm.com", "2.45.2", "2.45.2",
            ["gettext", "pcre2", "openssl@3"], false, "homebrew/core"));

    public Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BrewService>>(
        [
            new("postgresql@16", "started", "yves", "/opt/homebrew/.../postgresql@16.plist"),
            new("redis", "stopped", null, "/opt/homebrew/.../redis.plist"),
        ]);

    public Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
