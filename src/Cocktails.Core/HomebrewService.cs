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

    public async Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["info", "--json=v2", name], cancellationToken);
        return ParseInfo(result.StandardOutput, name);
    }

    public async Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["update"], cancellationToken, output);

    public async Task PinAsync(string name, CancellationToken cancellationToken = default)
        => await RunAsync(["pin", name], cancellationToken);

    public async Task UnpinAsync(string name, CancellationToken cancellationToken = default)
        => await RunAsync(["unpin", name], cancellationToken);

    public async Task<IReadOnlyList<string>> GetDependentsAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["uses", "--installed", name], cancellationToken);
        return [.. SplitLines(result.StandardOutput)];
    }

    public async Task<IReadOnlyList<string>> GetLeavesAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["leaves"], cancellationToken);
        return [.. SplitLines(result.StandardOutput)];
    }

    public async Task<BrewEnvironment> GetEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        var config = await RunAsync(["config"], cancellationToken);
        var values = ParseConfig(config.StandardOutput);
        var cache = await RunAsync(["--cache"], cancellationToken);

        return new BrewEnvironment(
            values.GetValueOrDefault("HOMEBREW_VERSION", "?"),
            values.GetValueOrDefault("HOMEBREW_PREFIX", _brewPath),
            cache.StandardOutput.Trim());
    }

    public async Task<bool> GetAnalyticsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(_brewPath, ["analytics"], null, cancellationToken).ConfigureAwait(false);
        return result.StandardOutput.Contains("are enabled", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetAnalyticsAsync(bool enabled, CancellationToken cancellationToken = default)
        => await RunAsync(["analytics", enabled ? "on" : "off"], cancellationToken);

    public async Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["install", name], cancellationToken, output);

    public async Task ReinstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["reinstall", name], cancellationToken, output);

    public async Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["uninstall", name], cancellationToken, output);

    public async Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default)
    {
        string[] args = name is null ? ["upgrade"] : ["upgrade", name];
        await RunAsync(args, cancellationToken, output);
    }

    public async Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["cleanup"], cancellationToken, output);

    public async Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["autoremove"], cancellationToken, output);

    public async Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
    {
        // brew doctor sort en code non nul quand il trouve des avertissements (normal) :
        // on ne passe pas par RunAsync (qui lèverait), on diffuse juste la sortie.
        await _runner.RunAsync(_brewPath, ["doctor"], output, cancellationToken).ConfigureAwait(false);
    }

    public async Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["bundle", "dump", "--file=" + path, "--force"], cancellationToken, output);

    public async Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["bundle", "install", "--file=" + path], cancellationToken, output);

    public async Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["services", "list", "--json"], cancellationToken);
        return ParseServices(result.StandardOutput);
    }

    public async Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["services", "start", name], cancellationToken, output);

    public async Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["services", "stop", name], cancellationToken, output);

    public async Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["services", "restart", name], cancellationToken, output);

    public async Task<IReadOnlyList<BrewTap>> GetTapsAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["tap-info", "--installed", "--json"], cancellationToken);
        return ParseTaps(result.StandardOutput);
    }

    public async Task AddTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["tap", name], cancellationToken, output);

    public async Task RemoveTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["untap", name], cancellationToken, output);

    public async Task TrustTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        => await RunAsync(["trust", name], cancellationToken, output);

    private async Task<ProcessResult> RunAsync(
        string[] args, CancellationToken cancellationToken, IProgress<string>? output = null)
    {
        var result = await _runner.RunAsync(_brewPath, args, output, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Parse la sortie JSON de <c>brew info --json=v2 &lt;name&gt;</c>. Le premier élément
    /// de la section <c>formulae</c> (sinon <c>casks</c>) est retenu.
    /// </summary>
    public static PackageDetails ParseInfo(string json, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("formulae", out var formulae)
                && formulae.ValueKind == JsonValueKind.Array && formulae.GetArrayLength() > 0)
            {
                return ParseFormulaInfo(formulae[0], fallbackName);
            }

            if (root.TryGetProperty("casks", out var casks)
                && casks.ValueKind == JsonValueKind.Array && casks.GetArrayLength() > 0)
            {
                return ParseCaskInfo(casks[0], fallbackName);
            }
        }

        return new PackageDetails(fallbackName, PackageKind.Formula, null, null, null, null, [], false, null);
    }

    private static PackageDetails ParseFormulaInfo(JsonElement f, string fallbackName)
    {
        string? stable = null;
        if (f.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Object)
        {
            stable = GetString(versions, "stable");
        }

        string? installed = null;
        if (f.TryGetProperty("installed", out var inst)
            && inst.ValueKind == JsonValueKind.Array && inst.GetArrayLength() > 0)
        {
            installed = GetString(inst[inst.GetArrayLength() - 1], "version");
        }

        var deps = new List<string>();
        if (f.TryGetProperty("dependencies", out var d) && d.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in d.EnumerateArray())
            {
                if (e.GetString() is { } s)
                {
                    deps.Add(s);
                }
            }
        }

        var pinned = f.TryGetProperty("pinned", out var p) && p.ValueKind == JsonValueKind.True;

        return new PackageDetails(
            GetString(f, "name") ?? fallbackName, PackageKind.Formula,
            GetString(f, "desc"), GetString(f, "homepage"),
            stable, installed, deps, pinned, GetString(f, "tap"));
    }

    private static PackageDetails ParseCaskInfo(JsonElement c, string fallbackName)
    {
        var deps = new List<string>();
        if (c.TryGetProperty("depends_on", out var dep) && dep.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in (ReadOnlySpan<string>)["formula", "cask"])
            {
                if (dep.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in arr.EnumerateArray())
                    {
                        if (e.GetString() is { } s)
                        {
                            deps.Add(s);
                        }
                    }
                }
            }
        }

        return new PackageDetails(
            GetString(c, "token") ?? fallbackName, PackageKind.Cask,
            GetString(c, "desc"), GetString(c, "homepage"),
            GetString(c, "version"), GetString(c, "installed"),
            deps, false, GetString(c, "tap"));
    }

    /// <summary>Parse la sortie JSON de <c>brew services list --json</c> (tableau d'objets).</summary>
    public static IReadOnlyList<BrewService> ParseServices(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var services = new List<BrewService>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var name = GetString(item, "name");
            if (name is null)
            {
                continue;
            }

            services.Add(new BrewService(
                name,
                GetString(item, "status") ?? "unknown",
                GetString(item, "user"),
                GetString(item, "file")));
        }

        return services;
    }

    /// <summary>Parse la sortie JSON de <c>brew tap-info --installed --json</c>.</summary>
    public static IReadOnlyList<BrewTap> ParseTaps(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var taps = new List<BrewTap>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var name = GetString(item, "name");
            if (name is null)
            {
                continue;
            }

            taps.Add(new BrewTap(
                name,
                IsTrue(item, "official"),
                ArrayLength(item, "formula_names"),
                ArrayLength(item, "cask_tokens"),
                IsTrue(item, "custom_remote")));
        }

        return taps;
    }

    /// <summary>Parse la sortie « CLÉ: valeur » de <c>brew config</c>.</summary>
    public static IReadOnlyDictionary<string, string> ParseConfig(string output)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in SplitLines(output))
        {
            var idx = line.IndexOf(": ", StringComparison.Ordinal);
            if (idx > 0)
            {
                values[line[..idx].Trim()] = line[(idx + 2)..].Trim();
            }
        }

        return values;
    }

    private static bool IsTrue(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.True;

    private static int ArrayLength(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.GetArrayLength()
            : 0;

    private static string? GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static IEnumerable<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
