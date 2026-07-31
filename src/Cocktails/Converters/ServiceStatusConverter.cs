using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Cocktails.Localization;

namespace Cocktails.Converters;

/// <summary>
/// Convertit l'état brut d'un service (<c>started</c>, <c>stopped</c>…) en libellé traduit
/// via la clé <c>Svc.&lt;status&gt;</c>. Repli sur la valeur brute pour un état inconnu.
/// </summary>
public sealed class ServiceStatusConverter : IValueConverter
{
    public static readonly ServiceStatusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string status || status.Length == 0)
        {
            return value;
        }

        var label = Localizer.Instance["Svc." + status];
        return label == "Svc." + status ? status : label;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
