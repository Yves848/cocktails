using System.Collections.Generic;
using Cocktails.Localization;

namespace Cocktails.ViewModels;

/// <summary>Écran « Aide » : raccourcis clavier et rappel des opérations par lot.</summary>
public sealed class HelpViewModel : ScreenViewModel
{
    public HelpViewModel() : base(new DesignHomebrewService())
    {
    }

    protected override string TitleKey => "Nav.Help";

    /// <summary>Groupes de raccourcis affichés (⌘ = touche Commande).</summary>
    public IReadOnlyList<ShortcutGroup> Groups { get; private set; } = BuildGroups();

    /// <summary>Rappels d'usage des opérations par lot.</summary>
    public IReadOnlyList<string> BatchTips { get; private set; } = BuildTips();

    protected override void OnLanguageChanged()
    {
        Groups = BuildGroups();
        BatchTips = BuildTips();
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(BatchTips));
    }

    private static IReadOnlyList<ShortcutGroup> BuildGroups() =>
    [
        new ShortcutGroup(L["Help.GroupNav"], [
            new Shortcut("⌘ ,", L["Help.OpenSettings"]),
            new Shortcut("F1", L["Help.OpenHelp"]),
            new Shortcut("↑ ↓", L["Help.BrowseList"]),
        ]),
        new ShortcutGroup(L["Help.GroupWindow"], [
            new Shortcut("⌘ W", L["Help.HideWindow"]),
            new Shortcut("⌘ M", L["Help.MinimizeWindow"]),
            new Shortcut("⌘ Q", L["Help.Quit"]),
        ]),
        new ShortcutGroup(L["Help.GroupSearch"], [
            new Shortcut("⏎", L["Help.LaunchSearch"]),
            new Shortcut(L["Help.KeyTyping"], L["Help.FilterLive"]),
        ]),
    ];

    private static IReadOnlyList<string> BuildTips() =>
    [
        L["Help.Batch1"],
        L["Help.Batch2"],
        L["Help.Batch3"],
        L["Help.Batch4"],
        L["Help.Batch5"],
    ];
}

/// <summary>Un raccourci : combinaison de touches + description.</summary>
public sealed record Shortcut(string Keys, string Description);

/// <summary>Un groupe nommé de raccourcis.</summary>
public sealed record ShortcutGroup(string Title, IReadOnlyList<Shortcut> Shortcuts);
