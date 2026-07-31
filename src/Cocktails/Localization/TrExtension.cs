using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Cocktails.Localization;

/// <summary>
/// Extension XAML de traduction : <c>{loc:Tr Cle}</c> se lie à <c>Localizer.Instance["Cle"]</c>
/// en OneWay, si bien qu'un changement de langue met à jour le texte à chaud.
/// </summary>
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key) => Key = key;

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new Binding($"[{Key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay,
        };
}
