using Cocktails.Core;
using Cocktails.Core.Models;

namespace Cocktails.Core.Tests;

public class HomebrewParsingTests
{
    [Fact]
    public void ParseInstalled_ReadsNameAndLastVersion()
    {
        const string output = """
            git 2.45.2
            openssl@3 3.3.1 3.3.0
            wget 1.24.5
            """;

        var packages = HomebrewService.ParseInstalled(output, PackageKind.Formula);

        Assert.Equal(3, packages.Count);
        Assert.Equal("git", packages[0].Name);
        Assert.Equal("2.45.2", packages[0].InstalledVersion);
        Assert.True(packages[0].IsInstalled);
        // Plusieurs versions installées : la dernière est retenue.
        Assert.Equal("openssl@3", packages[1].Name);
        Assert.Equal("3.3.0", packages[1].InstalledVersion);
        Assert.All(packages, p => Assert.Equal(PackageKind.Formula, p.Kind));
    }

    [Fact]
    public void ParseInstalled_ToleratesEntryWithoutVersion()
    {
        var packages = HomebrewService.ParseInstalled("somecask\n", PackageKind.Cask);

        var pkg = Assert.Single(packages);
        Assert.Equal("somecask", pkg.Name);
        Assert.Null(pkg.InstalledVersion);
        Assert.False(pkg.IsInstalled);
    }

    [Fact]
    public void ParseInstalledInfo_ReadsNameVersionAndHomepage()
    {
        // Forme réelle de `brew info --installed --json=v2` : sections formulae/casks.
        // Formula : version = dernier élément de "installed"[]. Cask : "installed" = string.
        const string json = """
            {
              "formulae": [
                {
                  "name": "jq",
                  "homepage": "https://jqlang.github.io/jq/",
                  "installed": [ { "version": "1.6" }, { "version": "1.7.1" } ]
                }
              ],
              "casks": [
                {
                  "token": "1password-cli",
                  "homepage": "https://1password.com/downloads/command-line/",
                  "installed": "2.38.1"
                }
              ]
            }
            """;

        var packages = HomebrewService.ParseInstalledInfo(json);

        Assert.Equal(2, packages.Count);

        var jq = packages[0];
        Assert.Equal("jq", jq.Name);
        Assert.Equal(PackageKind.Formula, jq.Kind);
        Assert.Equal("1.7.1", jq.InstalledVersion); // dernière version installée
        Assert.Equal("https://jqlang.github.io/jq/", jq.Homepage);

        var op = packages[1];
        Assert.Equal("1password-cli", op.Name);
        Assert.Equal(PackageKind.Cask, op.Kind);
        Assert.Equal("2.38.1", op.InstalledVersion);
        Assert.Equal("https://1password.com/downloads/command-line/", op.Homepage);
    }

    [Fact]
    public void ParseInstalledInfo_BlankReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseInstalledInfo(""));

    [Fact]
    public void ParseSearch_SplitsFormulaeAndCasksBySectionHeaders()
    {
        const string output = """
            ==> Formulae
            wget
            wget2
            ==> Casks
            wireshark
            """;

        var packages = HomebrewService.ParseSearch(output);

        Assert.Equal(3, packages.Count);
        Assert.Equal(PackageKind.Formula, packages[0].Kind);
        Assert.Equal(PackageKind.Formula, packages[1].Kind);
        Assert.Equal("wireshark", packages[2].Name);
        Assert.Equal(PackageKind.Cask, packages[2].Kind);
    }

    [Fact]
    public void ParseSearch_WithoutHeaders_DefaultsToFormula()
    {
        var packages = HomebrewService.ParseSearch("ripgrep\nfd\n");

        Assert.Equal(2, packages.Count);
        Assert.All(packages, p => Assert.Equal(PackageKind.Formula, p.Kind));
    }

    [Fact]
    public void ParseOutdated_ReadsInstalledAndCurrentVersions()
    {
        const string json = """
            {
              "formulae": [
                { "name": "git", "installed_versions": ["2.45.1"], "current_version": "2.45.2" }
              ],
              "casks": [
                { "name": "firefox", "installed_versions": ["127.0"], "current_version": "128.0" }
              ]
            }
            """;

        var packages = HomebrewService.ParseOutdated(json);

        Assert.Equal(2, packages.Count);

        var git = packages[0];
        Assert.Equal("git", git.Name);
        Assert.Equal(PackageKind.Formula, git.Kind);
        Assert.Equal("2.45.1", git.InstalledVersion);
        Assert.Equal("2.45.2", git.LatestVersion);
        Assert.True(git.IsOutdated);

        Assert.Equal(PackageKind.Cask, packages[1].Kind);
        Assert.True(packages[1].IsOutdated);
    }

    [Fact]
    public void ParseOutdated_EmptyOrBlank_ReturnsEmpty()
    {
        Assert.Empty(HomebrewService.ParseOutdated(""));
        Assert.Empty(HomebrewService.ParseOutdated("   "));
    }

    [Fact]
    public void ParseInfo_Formula_ReadsDescriptionDependenciesAndVersions()
    {
        const string json = """
            {
              "formulae": [
                {
                  "name": "git",
                  "tap": "homebrew/core",
                  "desc": "Distributed revision control system",
                  "homepage": "https://git-scm.com",
                  "versions": { "stable": "2.45.2" },
                  "installed": [ { "version": "2.45.1" } ],
                  "dependencies": ["gettext", "pcre2"],
                  "pinned": true
                }
              ],
              "casks": []
            }
            """;

        var d = HomebrewService.ParseInfo(json, "git");

        Assert.Equal("git", d.Name);
        Assert.Equal(PackageKind.Formula, d.Kind);
        Assert.Equal("Distributed revision control system", d.Description);
        Assert.Equal("https://git-scm.com", d.Homepage);
        Assert.Equal("2.45.2", d.StableVersion);
        Assert.Equal("2.45.1", d.InstalledVersion);
        Assert.Equal(["gettext", "pcre2"], d.Dependencies);
        Assert.True(d.IsPinned);
        Assert.True(d.IsOutdated);
        Assert.Equal("homebrew/core", d.Tap);
    }

    [Fact]
    public void ParseInfo_Cask_ReadsTokenVersionAndDependsOn()
    {
        const string json = """
            {
              "formulae": [],
              "casks": [
                {
                  "token": "firefox",
                  "name": ["Firefox"],
                  "desc": "Web browser",
                  "homepage": "https://mozilla.org",
                  "version": "128.0",
                  "installed": "127.0",
                  "tap": "homebrew/cask",
                  "depends_on": { "formula": ["gnupg"], "cask": ["rosetta"] }
                }
              ]
            }
            """;

        var d = HomebrewService.ParseInfo(json, "firefox");

        Assert.Equal("firefox", d.Name);
        Assert.Equal(PackageKind.Cask, d.Kind);
        Assert.Equal("128.0", d.StableVersion);
        Assert.Equal("127.0", d.InstalledVersion);
        Assert.Equal(["gnupg", "rosetta"], d.Dependencies);
        Assert.False(d.IsPinned);
    }

    [Fact]
    public void ParseInfo_BlankOrUnknown_ReturnsFallback()
    {
        var d = HomebrewService.ParseInfo("", "mystery");
        Assert.Equal("mystery", d.Name);
        Assert.Empty(d.Dependencies);
        Assert.Null(d.Description);
    }

    [Fact]
    public void ParseServices_ReadsNameStatusUserFile()
    {
        const string json = """
            [
              { "name": "postgresql@16", "status": "started", "user": "yves", "file": "/opt/homebrew/x/pg.plist", "exit_code": null },
              { "name": "sketchybar", "status": "none", "user": null, "file": "/opt/homebrew/x/sb.plist" }
            ]
            """;

        var services = HomebrewService.ParseServices(json);

        Assert.Equal(2, services.Count);
        Assert.Equal("postgresql@16", services[0].Name);
        Assert.True(services[0].IsRunning);
        Assert.Equal("yves", services[0].User);
        Assert.Equal("actif", services[0].StatusLabel);

        Assert.False(services[1].IsRunning);
        Assert.Null(services[1].User);
        Assert.Equal("inactif", services[1].StatusLabel);
    }

    [Fact]
    public void ParseServices_BlankReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseServices(""));

    [Fact]
    public void ParseTaps_ReadsNameOfficialAndCounts()
    {
        const string json = """
            [
              { "name": "homebrew/core", "official": true, "custom_remote": false,
                "trusted": true, "formula_names": ["a","b","c"], "cask_tokens": [] },
              { "name": "felixkratz/formulae", "official": false, "custom_remote": false,
                "trusted": true, "formula_names": ["sketchybar","borders"], "cask_tokens": ["x"] },
              { "name": "koekeishiya/formulae", "official": false, "custom_remote": false,
                "trusted": false, "formula_names": ["yabai"], "cask_tokens": [] }
            ]
            """;

        var taps = HomebrewService.ParseTaps(json);

        Assert.Equal(3, taps.Count);
        Assert.True(taps[0].Official);
        Assert.Equal("officiel", taps[0].KindLabel);
        Assert.Equal(3, taps[0].FormulaCount);
        Assert.True(taps[0].Trusted);
        Assert.False(taps[0].CanTrust);               // officiel → rien à approuver

        Assert.False(taps[1].Official);
        Assert.Equal(2, taps[1].FormulaCount);
        Assert.Equal(1, taps[1].CaskCount);
        Assert.True(taps[1].Trusted);                 // tiers déjà de confiance
        Assert.False(taps[1].CanTrust);

        Assert.False(taps[2].Trusted);                // tiers non approuvé
        Assert.True(taps[2].CanTrust);                // → bouton « Confiance » proposé
    }

    [Fact]
    public void ParseTaps_BlankReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseTaps(""));

    [Fact]
    public void ParseDepsTree_ReadsDepthsAndNames()
    {
        // Sortie réelle de `brew deps --tree git`.
        const string output = "git\n├── pcre2\n└── gettext\n    ├── json-c\n    └── libunistring\n";

        var nodes = HomebrewService.ParseDepsTree(output);

        Assert.Equal(5, nodes.Count);
        Assert.Equal(new DependencyNode(0, "git"), nodes[0]);
        Assert.True(nodes[0].IsRoot);
        Assert.Equal(new DependencyNode(1, "pcre2"), nodes[1]);
        Assert.Equal(new DependencyNode(1, "gettext"), nodes[2]);
        Assert.Equal(new DependencyNode(2, "json-c"), nodes[3]);
        Assert.Equal(new DependencyNode(2, "libunistring"), nodes[4]);
    }

    [Fact]
    public void ParseDepsTree_HandlesVerticalBarsAndDepthThree()
    {
        // Sortie réelle de `brew deps --tree wget` (extrait avec barres « │ »).
        const string output =
            "wget\n" +
            "├── libidn2\n" +
            "│   ├── libunistring\n" +
            "│   └── gettext\n" +
            "│       ├── json-c\n" +
            "│       └── libunistring\n" +
            "├── libpsl\n" +
            "└── openssl@3\n" +
            "    └── ca-certificates\n";

        var nodes = HomebrewService.ParseDepsTree(output);

        Assert.Equal(new DependencyNode(0, "wget"), nodes[0]);
        Assert.Equal(new DependencyNode(1, "libidn2"), nodes[1]);
        Assert.Equal(new DependencyNode(2, "libunistring"), nodes[2]);
        Assert.Equal(new DependencyNode(2, "gettext"), nodes[3]);
        Assert.Equal(new DependencyNode(3, "json-c"), nodes[4]);
        Assert.Equal(new DependencyNode(3, "libunistring"), nodes[5]);
        // openssl@3 (racine niveau 1) et sa dépendance (niveau 2).
        Assert.Contains(new DependencyNode(1, "openssl@3"), nodes);
        Assert.Contains(new DependencyNode(2, "ca-certificates"), nodes);
    }

    [Fact]
    public void ParseDepsTree_RootOnly_IsNotShownAsTree()
    {
        var nodes = HomebrewService.ParseDepsTree("pcre2\n");

        var node = Assert.Single(nodes);
        Assert.Equal(new DependencyNode(0, "pcre2"), node);

        // Un détail sans dépendance transitive ne déclenche pas la section.
        var details = new PackageDetails(
            "pcre2", PackageKind.Formula, null, null, null, null, [], false, null, nodes);
        Assert.False(details.HasDependencyTree);
    }

    [Fact]
    public void ParseDepsTree_Blank_ReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseDepsTree(""));

    [Fact]
    public void ParseMissing_ReadsFormulaAndMissingDeps()
    {
        // Format `brew missing` : « formule: dep1 dep2 » par ligne.
        const string output = """
            wget: openssl@3 libidn2
            curl: openssl@3
            """;

        var missing = HomebrewService.ParseMissing(output);

        Assert.Equal(2, missing.Count);
        Assert.Equal("wget", missing[0].Formula);
        Assert.Equal(["openssl@3", "libidn2"], missing[0].Missing);
        Assert.Equal("curl", missing[1].Formula);
        Assert.Equal(["openssl@3"], missing[1].Missing);
    }

    [Fact]
    public void ParseMissing_Blank_ReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseMissing(""));

    [Fact]
    public void ParseConfig_ReadsKeyValuePairs()
    {
        const string output = """
            HOMEBREW_VERSION: 6.0.13
            ORIGIN: https://github.com/Homebrew/brew
            HOMEBREW_PREFIX: /opt/homebrew
            """;

        var config = HomebrewService.ParseConfig(output);

        Assert.Equal("6.0.13", config["HOMEBREW_VERSION"]);
        Assert.Equal("/opt/homebrew", config["HOMEBREW_PREFIX"]);
        // La valeur peut contenir « : » (URL) sans casser le parsing.
        Assert.Equal("https://github.com/Homebrew/brew", config["ORIGIN"]);
    }
}
