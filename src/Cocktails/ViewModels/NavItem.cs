using Cocktails.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>Entrée de la navigation latérale : libellé, icône, écran, et compteur optionnel.</summary>
public sealed partial class NavItem : ObservableObject
{
    private readonly string _titleKey;

    public NavItem(string titleKey, string icon, ScreenViewModel screen, string color = "#8A93A6")
    {
        _titleKey = titleKey;
        Icon = icon;
        Screen = screen;
        Color = color;
        Localizer.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }

    /// <summary>Libellé traduit (mis à jour à chaud au changement de langue).</summary>
    public string Title => Localizer.Instance[_titleKey];

    /// <summary>Clé de traduction du libellé (utilisée par le shell pour la sélection par nom).</summary>
    public string TitleKey => _titleKey;

    /// <summary>
    /// Données de tracé de l'icône (mini-langage SVG path). Converties en géométrie
    /// à l'affichage par <c>StringToGeometryConverter</c> — pas à la construction, pour
    /// que les view models restent indépendants de la plateforme Avalonia (testables).
    /// </summary>
    public string Icon { get; }

    /// <summary>Couleur d'accent propre à l'onglet (hex), pour distinguer les icônes.</summary>
    public string Color { get; }

    public ScreenViewModel Screen { get; }

    /// <summary>Compteur affiché en badge (0 = pas de badge). Ex. mises à jour disponibles.</summary>
    [ObservableProperty]
    public partial int Count { get; set; }
}
