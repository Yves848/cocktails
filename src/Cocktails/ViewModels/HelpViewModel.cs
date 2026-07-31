using System.Collections.Generic;

namespace Cocktails.ViewModels;

/// <summary>Écran « Aide » : raccourcis clavier et rappel des opérations par lot.</summary>
public sealed class HelpViewModel : ScreenViewModel
{
    public HelpViewModel() : base(new DesignHomebrewService())
    {
    }

    public override string Title => "Aide";

    /// <summary>Groupes de raccourcis affichés (⌘ = touche Commande).</summary>
    public IReadOnlyList<ShortcutGroup> Groups { get; } =
    [
        new ShortcutGroup("Navigation", [
            new Shortcut("⌘ ,", "Ouvrir les Réglages"),
            new Shortcut("F1", "Ouvrir cette aide"),
            new Shortcut("↑ ↓", "Parcourir la liste sélectionnée"),
        ]),
        new ShortcutGroup("Fenêtre", [
            new Shortcut("⌘ W", "Masquer la fenêtre (l'app reste en arrière-plan)"),
            new Shortcut("⌘ M", "Réduire la fenêtre"),
            new Shortcut("⌘ Q", "Quitter Cocktails"),
        ]),
        new ShortcutGroup("Recherche & filtres", [
            new Shortcut("⏎", "Lancer la recherche (dans le champ Rechercher)"),
            new Shortcut("Saisie", "Filtrer la liste en direct (champ Filtrer)"),
        ]),
    ];

    /// <summary>Rappels d'usage des opérations par lot.</summary>
    public IReadOnlyList<string> BatchTips { get; } =
    [
        "Cochez plusieurs lignes dans « Installés » ou « Mises à jour » à l'aide des cases.",
        "Une barre d'actions apparaît alors en haut de la liste avec le nombre sélectionné.",
        "« Mettre à jour la sélection » lance brew upgrade sur toutes les lignes cochées.",
        "« Désinstaller la sélection » enchaîne les désinstallations (confirmation d'abord).",
        "« Tout décocher » remet la sélection à zéro sans rien lancer.",
    ];
}

/// <summary>Un raccourci : combinaison de touches + description.</summary>
public sealed record Shortcut(string Keys, string Description);

/// <summary>Un groupe nommé de raccourcis.</summary>
public sealed record ShortcutGroup(string Title, IReadOnlyList<Shortcut> Shortcuts);
