using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>Entrée de la navigation latérale : libellé, icône, écran, et compteur optionnel.</summary>
public sealed partial class NavItem : ObservableObject
{
    public NavItem(string title, string icon, ScreenViewModel screen)
    {
        Title = title;
        Icon = icon;
        Screen = screen;
    }

    public string Title { get; }

    /// <summary>
    /// Données de tracé de l'icône (mini-langage SVG path). Converties en géométrie
    /// à l'affichage par <c>StringToGeometryConverter</c> — pas à la construction, pour
    /// que les view models restent indépendants de la plateforme Avalonia (testables).
    /// </summary>
    public string Icon { get; }

    public ScreenViewModel Screen { get; }

    /// <summary>Compteur affiché en badge (0 = pas de badge). Ex. mises à jour disponibles.</summary>
    [ObservableProperty]
    public partial int Count { get; set; }
}
