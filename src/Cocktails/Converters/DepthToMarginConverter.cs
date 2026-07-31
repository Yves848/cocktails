using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Cocktails.Converters;

/// <summary>
/// Convertit une profondeur d'arbre (int) en marge gauche, pour indenter visuellement
/// les nœuds de l'arbre de dépendances. Chaque niveau vaut 16 px.
/// </summary>
public sealed class DepthToMarginConverter : IValueConverter
{
    public static readonly DepthToMarginConverter Instance = new();

    private const double Step = 16;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new Thickness(value is int depth ? depth * Step : 0, 0, 0, 0);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
