using System;
using System.Collections.Generic;

namespace Cocktails.ViewModels;

/// <summary>
/// Options de ligne de commande. Elles servent à amener l'application dans un état
/// déterministe au lancement — en pratique pour les captures d'écran du site, où piloter
/// l'interface au clavier n'est pas fiable (un lancement = un écran).
/// <para>
/// <c>Cocktails --screen installed --select cairo</c>
/// </para>
/// </summary>
/// <param name="ScreenKey">Clé de navigation de l'écran à afficher, ou <c>null</c>.</param>
/// <param name="SelectPackage">Paquet à sélectionner dans la liste, ou <c>null</c>.</param>
public sealed record StartupOptions(string? ScreenKey, string? SelectPackage)
{
    /// <summary>Noms acceptés par <c>--screen</c> et leur clé de navigation.</summary>
    private static readonly Dictionary<string, string> Screens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["installed"] = "Nav.Installed",
        ["search"] = "Nav.Search",
        ["updates"] = "Nav.Updates",
        ["maintenance"] = "Nav.Maintenance",
        ["services"] = "Nav.Services",
        ["taps"] = "Nav.Taps",
        ["settings"] = "Nav.Settings",
        ["help"] = "Nav.Help",
    };

    /// <summary>
    /// Lit les options reconnues et ignore tout le reste : macOS ajoute ses propres
    /// arguments (<c>-psn_…</c>) au lancement d'un bundle.
    /// </summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        string? screenKey = null;
        string? selectPackage = null;

        for (var i = 0; i < args.Count; i++)
        {
            var value = i + 1 < args.Count ? args[i + 1] : null;
            switch (args[i])
            {
                case "--screen" when value is not null && Screens.TryGetValue(value, out var key):
                    screenKey = key;
                    break;
                case "--select" when value is not null:
                    selectPackage = value;
                    break;
            }
        }

        return new StartupOptions(screenKey, selectPackage);
    }
}
