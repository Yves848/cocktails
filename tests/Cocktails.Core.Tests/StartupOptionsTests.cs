using System.Threading.Tasks;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

/// <summary>
/// Options de ligne de commande, utilisées pour amener l'app dans un état déterministe
/// (captures d'écran du site : un lancement = un écran, sans piloter l'interface).
/// </summary>
public class StartupOptionsTests
{
    [Theory]
    [InlineData("installed", "Nav.Installed")]
    [InlineData("search", "Nav.Search")]
    [InlineData("updates", "Nav.Updates")]
    [InlineData("maintenance", "Nav.Maintenance")]
    [InlineData("services", "Nav.Services")]
    [InlineData("taps", "Nav.Taps")]
    [InlineData("settings", "Nav.Settings")]
    [InlineData("help", "Nav.Help")]
    public void Screen_MapsToItsNavigationKey(string value, string expected)
        => Assert.Equal(expected, StartupOptions.Parse(["--screen", value]).ScreenKey);

    [Fact]
    public void ScreenName_IsCaseInsensitive()
        => Assert.Equal("Nav.Taps", StartupOptions.Parse(["--screen", "TAPS"]).ScreenKey);

    [Fact]
    public void NoArguments_SelectsNothing()
        => Assert.Null(StartupOptions.Parse([]).ScreenKey);

    [Fact]
    public void UnknownScreen_IsIgnored()
        => Assert.Null(StartupOptions.Parse(["--screen", "nope"]).ScreenKey);

    [Fact]
    public void ScreenWithoutValue_IsIgnored()
        => Assert.Null(StartupOptions.Parse(["--screen"]).ScreenKey);

    [Fact]
    public void Select_CarriesThePackageToHighlight()
    {
        var options = StartupOptions.Parse(["--screen", "installed", "--select", "cairo"]);

        Assert.Equal("Nav.Installed", options.ScreenKey);
        Assert.Equal("cairo", options.SelectPackage);
    }

    [Fact]
    public void SelectWithoutValue_IsIgnored()
        => Assert.Null(StartupOptions.Parse(["--select"]).SelectPackage);

    [Fact]
    public async Task SelectByName_HighlightsThePackageAndLoadsItsDetails()
    {
        var screen = new InstalledViewModel(new DesignHomebrewService());
        await screen.ActivateAsync();

        var found = screen.SelectByName("ripgrep");

        Assert.True(found);
        Assert.Equal("ripgrep", screen.SelectedItem?.Package.Name);
        Assert.Equal("ripgrep", screen.SelectedPackage?.Name);
    }

    [Fact]
    public async Task SelectByName_UnknownPackage_ChangesNothing()
    {
        var screen = new InstalledViewModel(new DesignHomebrewService());
        await screen.ActivateAsync();

        Assert.False(screen.SelectByName("inexistant"));
        Assert.Null(screen.SelectedItem);
    }

    [Fact]
    public void UnrelatedArguments_AreIgnored()
    {
        // Le premier argument est le chemin de l'exécutable quand l'app est lancée par macOS.
        var options = StartupOptions.Parse(["/Applications/Cocktails.app", "-psn_0_12345"]);

        Assert.Null(options.ScreenKey);
        Assert.Null(options.SelectPackage);
    }
}
