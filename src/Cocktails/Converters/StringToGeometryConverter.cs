using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cocktails.Converters;

/// <summary>
/// Convertit une chaîne de tracé (mini-langage SVG path) en <see cref="Geometry"/>.
/// Utilisé pour les icônes de navigation : le parsing a lieu à l'affichage (plateforme
/// Avalonia initialisée), pas dans les view models.
/// </summary>
public sealed class StringToGeometryConverter : IValueConverter
{
    public static readonly StringToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s.Length > 0 ? StreamGeometry.Parse(s) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
