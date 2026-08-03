using Cocktails.Services;

namespace Cocktails.Core.Tests;

public class BrewCommandLineTests
{
    [Theory]
    [InlineData("install wget", new[] { "install", "wget" })]
    [InlineData("  info   git  ", new[] { "info", "git" })]
    [InlineData("brew install wget", new[] { "install", "wget" })]      // préfixe brew retiré
    [InlineData("BREW list --versions", new[] { "list", "--versions" })] // insensible à la casse
    [InlineData("search openssl@3", new[] { "search", "openssl@3" })]    // @ conservé
    [InlineData("tap homebrew/cask-fonts", new[] { "tap", "homebrew/cask-fonts" })] // / conservé
    public void Parse_ValidCommands(string input, string[] expected)
        => Assert.Equal(expected, BrewCommandLine.Parse(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("brew")]
    [InlineData("brew   ")]
    [InlineData("install wget; rm -rf /")]   // point-virgule
    [InlineData("cat /etc/passwd | mail")]   // pipe
    [InlineData("install $(whoami)")]        // substitution
    [InlineData("info git && curl evil")]    // enchaînement
    [InlineData("list > /tmp/out")]          // redirection
    [InlineData("echo `id`")]                // backticks
    public void Parse_RejectsEmptyOrShellOperators(string input)
        => Assert.Null(BrewCommandLine.Parse(input));

    [Theory]
    [InlineData("install", true)]
    [InlineData("uninstall", true)]
    [InlineData("upgrade", true)]
    [InlineData("reinstall", true)]
    [InlineData("tap", true)]
    [InlineData("info", false)]
    [InlineData("list", false)]
    [InlineData("search", false)]
    [InlineData("deps", false)]
    public void IsMutating_DetectsStateChangingSubcommands(string sub, bool expected)
        => Assert.Equal(expected, BrewCommandLine.IsMutating([sub, "x"]));

    [Fact]
    public void SuggestedCommands_ExtractsRunnableBrewLines()
    {
        // Sortie type de brew quand un nom est ambigu (formule + cask).
        string[] output =
        [
            "To install firefly, run:",
            "  brew install firefly",
            "",
            "==> Casks",
            "firefly-iota-desktop",
            "firefox",
            "",
            "To install firefly-iota-desktop, run:",
            "  brew install --cask firefly-iota-desktop",
            "✗ exit 1",
        ];

        var suggestions = BrewCommandLine.SuggestedCommands(output);

        Assert.Equal(
            ["brew install firefly", "brew install --cask firefly-iota-desktop"],
            suggestions);
    }

    [Fact]
    public void SuggestedCommands_HandlesBacktickHint()
        => Assert.Equal(
            ["brew install --cask firefox"],
            BrewCommandLine.SuggestedCommands(["It exists as a Cask. Try `brew install --cask firefox`"]));

    [Fact]
    public void OptionsFor_ListIncludesVersions_AndCommonFlags()
    {
        var options = BrewCommandLine.OptionsFor("list");
        Assert.Contains("--versions", options);
        Assert.Contains("--cask", options);
        Assert.Contains("--help", options);   // options communes ajoutées
    }

    [Fact]
    public void OptionsFor_UnknownSubcommand_ReturnsCommonOnly()
        => Assert.Equal(["--help", "--verbose", "--debug", "--quiet"], BrewCommandLine.OptionsFor("zork"));

    [Fact]
    public void CommonPrefix_ReturnsLongestSharedStart()
    {
        Assert.Equal("git-", BrewCommandLine.CommonPrefix(["git-lfs", "git-delta", "git-extras"]));
        Assert.Equal("node", BrewCommandLine.CommonPrefix(["node", "node@18", "node@20"]));
        Assert.Equal("", BrewCommandLine.CommonPrefix(["wget", "curl"]));
        Assert.Equal("wget", BrewCommandLine.CommonPrefix(["wget"]));
    }
}
