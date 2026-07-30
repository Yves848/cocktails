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
                "formula_names": ["a","b","c"], "cask_tokens": [] },
              { "name": "felixkratz/formulae", "official": false, "custom_remote": false,
                "formula_names": ["sketchybar","borders"], "cask_tokens": ["x"] }
            ]
            """;

        var taps = HomebrewService.ParseTaps(json);

        Assert.Equal(2, taps.Count);
        Assert.True(taps[0].Official);
        Assert.Equal("officiel", taps[0].KindLabel);
        Assert.Equal(3, taps[0].FormulaCount);

        Assert.False(taps[1].Official);
        Assert.Equal(2, taps[1].FormulaCount);
        Assert.Equal(1, taps[1].CaskCount);
    }

    [Fact]
    public void ParseTaps_BlankReturnsEmpty()
        => Assert.Empty(HomebrewService.ParseTaps(""));

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
