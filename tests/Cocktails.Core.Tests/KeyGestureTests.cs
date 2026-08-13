using Avalonia.Input;

namespace Cocktails.Core.Tests;

/// <summary>
/// Vérifie que le format de raccourci stocké (AppSettings.TerminalShortcut) est bien
/// interprété par Avalonia — c'est ce qui fait fonctionner la correspondance au clavier.
/// </summary>
public class KeyGestureTests
{
    [Fact]
    public void Parse_DefaultCmdT_YieldsMetaPlusT()
    {
        var gesture = KeyGesture.Parse("Cmd+T");
        Assert.Equal(Key.T, gesture.Key);
        Assert.True(gesture.KeyModifiers.HasFlag(KeyModifiers.Meta));
    }

    [Fact]
    public void RoundTrip_ConstructedGesture_ReparsesToSameKeyAndModifiers()
    {
        var original = new KeyGesture(Key.J, KeyModifiers.Meta | KeyModifiers.Shift);
        var reparsed = KeyGesture.Parse(original.ToString());

        Assert.Equal(original.Key, reparsed.Key);
        Assert.Equal(original.KeyModifiers, reparsed.KeyModifiers);
    }
}
