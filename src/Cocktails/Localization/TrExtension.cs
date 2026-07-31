using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Cocktails.Localization;

/// <summary>
/// Extension XAML de traduction : <c>{loc:Tr Cle}</c>. Se lie à <c>Localizer.Current</c>
/// (propriété classique) et traduit la clé via un converter : un changement de langue
/// émet <c>PropertyChanged(Current)</c>, ce qui rafraîchit le texte à chaud — y compris
/// sur la vue déjà affichée (contrairement à une liaison d'indexeur).
/// </summary>
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key) => Key = key;

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new Binding(nameof(Localizer.Current))
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay,
            Converter = LocalizeConverter.Instance,
            ConverterParameter = Key,
        };
}

/// <summary>Traduit une clé (passée en paramètre) dans la langue courante.</summary>
public sealed class LocalizeConverter : IValueConverter
{
    public static readonly LocalizeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Localizer.Instance[parameter as string ?? string.Empty];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
