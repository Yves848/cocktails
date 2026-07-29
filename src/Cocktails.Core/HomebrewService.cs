using System.Text.Json;
using Cocktails.Core.Models;
using Cocktails.Core.Process;

namespace Cocktails.Core;

/// <summary>
/// Implémentation de <see cref="IHomebrewService"/> qui pilote la CLI <c>brew</c>
/// via un <see cref="IProcessRunner"/>. Les méthodes de parsing sont statiques et
/// publiques pour être testables directement à partir de sorties <c>brew</c> capturées,
/// sans lancer de processus.
/// </summary>
public sealed class HomebrewService : IHomebrewService
{
    private readonly IProcessRunner _runner;
    private readonly string _brewPath;

    /// <param name="runner">Exécuteur de processus (réel ou factice pour les tests).</param>
    /// <param name="brewPath">
    /// Chemin de l'exécutable <c>brew</c>. Défaut : <c>/opt/homebrew/bin/brew</c>
    /// (emplacement standard sur Apple Silicon ; <c>/usr/local/bin/brew</c> sur Intel).
    /// </param>
    public HomebrewService(IProcessRunner runner, string brewPath = "/opt/homebrew/bin/brew")
    {
        _runner = runner;
        _brewPath = brewPath;
    }

    public async Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
    {
        var formulae = await RunAsync(["list", "--versions", "--formula"], cancellationToken);
        var casks = await RunAsync(["list", "--versions", "--cask"], cancellationToken);

        var packages = new List<Package>();
        packages.AddRange(ParseInstalled(formulae.StandardOutput, PackageKind.Formula));
        packages.AddRange(ParseInstalled(casks.StandardOutput, PackageKind.Cask));
        return packages;
    }

    public async Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["search", query], cancellationToken);
        return ParseSearch(result.StandardOutput);
    }

    public async Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["outdated", "--json=v2"], cancellationToken);
        return ParseOutdated(result.StandardOutput);
    }

    public async Task InstallAsync(string name, CancellationToken cancellationToken = default)
        => await RunAsync(["install", name], cancellationToken);

    public async Task UninstallAsync(string name, CancellationToken cancellationToken = default)
        => await RunAsync(["uninstall", name], cancellationToken);

    public async Task UpgradeAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        string[] args = name is null ? ["upgrade"] : ["upgrade", name];
        await RunAsync(args, cancellationToken);
    }

    private async Task<ProcessResult> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(_brewPath, args, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new HomebrewException(string.Join(' ', args), result);
        }

        return result;
    }

    // --- Parsing (statique et testable) ---------------------------------------

    /// <summary>
    /// Parse la sortie de <c>brew list --versions [--formula|--cask]</c>.
    /// Chaque ligne : « nom version [version…] ». La dernière version listée est
    /// retenue comme version installée courante.
    /// </summary>
    public static IReadOnlyList<Package> ParseInstalled(string output, PackageKind kind)
    {
        var packages = new List<Package>();
        foreach (var line in SplitLines(output))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var version = parts.Length > 1 ? parts[^1] : null;
            packages.Add(new Package(parts[0], kind, InstalledVersion: version));
        }

        return packages;
    }

    /// <summary>
    /// Parse la sortie de <c>brew search</c>. Les en-têtes de section
    /// (<c>==&gt; Formulae</c> / <c>==&gt; Casks</c>) fixent la nature des lignes
    /// suivantes ; en leur absence, tout est considéré comme formula.
    /// </summary>
    public static IReadOnlyList<Package> ParseSearch(string output)
    {
        var packages = new List<Package>();
        var kind = PackageKind.Formula;

        foreach (var line in SplitLines(output))
        {
            if (line.StartsWith("==>", StringComparison.Ordinal))
            {
                kind = line.Contains("Cask", StringComparison.OrdinalIgnoreCase)
                    ? PackageKind.Cask
                    : PackageKind.Formula;
                continue;
            }

            // Piped, brew search sort un nom par ligne. On reste tolérant à un
            // éventuel format colonné en découpant sur les espaces.
            foreach (var name in line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                packages.Add(new Package(name, kind));
            }
        }

        return packages;
    }

    /// <summary>
    /// Parse la sortie JSON de <c>brew outdated --json=v2</c> (sections
    /// <c>formulae</c> et <c>casks</c>).
    /// </summary>
    public static IReadOnlyList<Package> ParseOutdated(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var packages = new List<Package>();

        packages.AddRange(ParseOutdatedSection(root, "formulae", PackageKind.Formula));
        packages.AddRange(ParseOutdatedSection(root, "casks", PackageKind.Cask));
        return packages;
    }

    private static IEnumerable<Package> ParseOutdatedSection(JsonElement root, string sectionName, PackageKind kind)
    {
        if (!root.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in section.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name is null)
            {
                continue;
            }

            string? installed = null;
            if (item.TryGetProperty("installed_versions", out var iv)
                && iv.ValueKind == JsonValueKind.Array
                && iv.GetArrayLength() > 0)
            {
                installed = iv[iv.GetArrayLength() - 1].GetString();
            }

            var latest = item.TryGetProperty("current_version", out var cv) ? cv.GetString() : null;
            yield return new Package(name, kind, InstalledVersion: installed, LatestVersion: latest);
        }
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
