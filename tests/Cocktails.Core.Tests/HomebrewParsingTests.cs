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
}
