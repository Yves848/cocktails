using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cocktails.Converters;

/// <summary>Convertit un booléen en graisse de police (<c>true</c> → gras).</summary>
public sealed class BoolToBoldConverter : IValueConverter
{
    public static readonly BoolToBoldConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.SemiBold : FontWeight.Normal;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
