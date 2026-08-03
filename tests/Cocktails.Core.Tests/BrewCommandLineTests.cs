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
    public void CommonPrefix_ReturnsLongestSharedStart()
    {
        Assert.Equal("git-", BrewCommandLine.CommonPrefix(["git-lfs", "git-delta", "git-extras"]));
        Assert.Equal("node", BrewCommandLine.CommonPrefix(["node", "node@18", "node@20"]));
        Assert.Equal("", BrewCommandLine.CommonPrefix(["wget", "curl"]));
        Assert.Equal("wget", BrewCommandLine.CommonPrefix(["wget"]));
    }
}
