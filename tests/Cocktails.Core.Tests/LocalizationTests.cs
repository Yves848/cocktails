using Cocktails.Localization;

namespace Cocktails.Core.Tests;

public class LocalizationTests
{
    [Fact]
    public void Strings_TranslatesEachLanguage()
    {
        Assert.Equal("Installed", Strings.Get("Nav.Installed", AppLanguage.English));
        Assert.Equal("Installés", Strings.Get("Nav.Installed", AppLanguage.French));
        Assert.Equal("Instalados", Strings.Get("Nav.Installed", AppLanguage.Spanish));
        Assert.Equal("Installiert", Strings.Get("Nav.Installed", AppLanguage.German));
    }

    [Fact]
    public void Strings_UnknownKey_ReturnsKey()
        => Assert.Equal("Nope.Missing", Strings.Get("Nope.Missing", AppLanguage.English));

    [Fact]
    public void Localizer_SwitchesLanguageLive_ThenResets()
    {
        try
        {
            Localizer.Instance.SetLanguage(AppLanguage.English);
            Assert.Equal(AppLanguage.English, Localizer.Instance.Current);
            Assert.Equal("Settings", Localizer.Instance["Nav.Settings"]);
            Assert.Equal("2 package(s) installed.", Localizer.Instance.Format("Status.InstalledCount", 2));

            Localizer.Instance.SetLanguage(AppLanguage.German);
            Assert.Equal("Einstellungen", Localizer.Instance["Nav.Settings"]);
        }
        finally
        {
            // Restaure l'état par défaut des tests (français) — singleton partagé.
            Localizer.Instance.SetLanguage(AppLanguage.French);
        }
    }

    [Fact]
    public void Localizer_RaisesLanguageChanged_OnRealChange()
    {
        var fired = 0;
        void Handler(object? s, System.EventArgs e) => fired++;
        Localizer.Instance.LanguageChanged += Handler;
        try
        {
            Localizer.Instance.SetLanguage(AppLanguage.English);   // fr → en : un événement
            Localizer.Instance.SetLanguage(AppLanguage.English);   // en → en : aucun
            Assert.Equal(1, fired);
        }
        finally
        {
            Localizer.Instance.LanguageChanged -= Handler;
            Localizer.Instance.SetLanguage(AppLanguage.French);
        }
    }
}
