using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cocktails.Converters;

/// <summary>
/// Convertit une chaîne de couleur (hex, ex. <c>#2DD4BF</c>) en <see cref="IBrush"/>.
/// Sert aux icônes de navigation colorées : la couleur reste une chaîne côté view model
/// (indépendant d'Avalonia) et n'est convertie qu'à l'affichage. Les pinceaux sont mis
/// en cache par couleur pour éviter de réallouer à chaque rendu.
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public static readonly StringToBrushConverter Instance = new();

    private static readonly System.Collections.Generic.Dictionary<string, IBrush> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || s.Length == 0)
        {
            return null;
        }

        if (!Cache.TryGetValue(s, out var brush))
        {
            brush = new SolidColorBrush(Color.Parse(s));
            Cache[s] = brush;
        }

        return brush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
