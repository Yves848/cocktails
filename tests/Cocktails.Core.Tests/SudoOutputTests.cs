using Cocktails.Core.Sudo;

namespace Cocktails.Core.Tests;

/// <summary>
/// Reconnaissance des lignes par lesquelles sudo signale qu'il n'a pas pu demander le mot
/// de passe. Sorties réelles capturées lors d'un <c>brew upgrade dotnet-sdk</c>.
/// </summary>
public class SudoOutputTests
{
    [Theory]
    [InlineData("sudo: a terminal is required to read the password; either use the -S option to read from standard input or configure an askpass helper")]
    [InlineData("sudo: a password is required")]
    public void RecognisesSudoPasswordFailures(string line)
        => Assert.True(SudoOutput.IsPasswordFailure(line));

    [Theory]
    [InlineData("==> Running installer for dotnet-sdk with `sudo` (which may request your password)...")]
    [InlineData("==> Linking Binary 'dotnet' to '/opt/homebrew/bin/dotnet'")]
    [InlineData("Warning: Reverting upgrade for Cask dotnet-sdk")]
    [InlineData("")]
    public void IgnoresOrdinaryOutput(string line)
        => Assert.False(SudoOutput.IsPasswordFailure(line));
}
