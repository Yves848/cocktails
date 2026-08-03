using System;
using System.Collections.Generic;

namespace Cocktails.Services;

/// <summary>
/// Analyse une saisie du terminal intégré en arguments pour le binaire <c>brew</c>.
/// Le terminal n'exécute <b>que</b> des sous-commandes brew : les arguments sont passés
/// tels quels au processus (aucun shell), et les métacaractères shell sont refusés pour
/// éviter toute confusion (ils ne seraient de toute façon pas interprétés).
/// </summary>
public static class BrewCommandLine
{
    // Opérateurs shell refusés (pas de pipeline / redirection / substitution).
    private static readonly char[] ShellOperators = [';', '|', '&', '<', '>', '$', '`', '(', ')', '{', '}', '\n', '\r'];

    // Sous-commandes qui modifient l'état → l'écran courant doit être rechargé ensuite.
    private static readonly HashSet<string> Mutating = new(StringComparer.OrdinalIgnoreCase)
    {
        "install", "uninstall", "remove", "rm", "upgrade", "reinstall", "pin", "unpin",
        "tap", "untap", "link", "unlink", "cleanup", "autoremove", "bundle",
    };

    /// <summary>
    /// Découpe la saisie en arguments brew. Retire un <c>brew</c> de tête éventuel.
    /// Retourne <c>null</c> si la saisie est vide ou contient un opérateur shell.
    /// </summary>
    public static string[]? Parse(string input)
    {
        var s = (input ?? string.Empty).Trim();
        if (s.Length == 0)
        {
            return null;
        }

        // « brew … » saisi explicitement : on retire le préfixe.
        if (s.Equals("brew", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (s.StartsWith("brew ", StringComparison.OrdinalIgnoreCase))
        {
            s = s[5..].Trim();
        }

        if (s.Length == 0 || s.IndexOfAny(ShellOperators) >= 0)
        {
            return null;
        }

        var args = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return args.Length == 0 ? null : args;
    }

    /// <summary>Vrai si la première sous-commande modifie l'état (install, uninstall…).</summary>
    public static bool IsMutating(IReadOnlyList<string> args)
        => args.Count > 0 && Mutating.Contains(args[0]);

    /// <summary>
    /// Sous-commandes brew usuelles proposées à la complétion du premier mot.
    /// </summary>
    public static readonly string[] Subcommands =
    [
        "install", "uninstall", "reinstall", "upgrade", "info", "search", "list", "deps",
        "uses", "pin", "unpin", "tap", "untap", "outdated", "cleanup", "autoremove", "doctor",
        "services", "link", "unlink", "home", "desc", "leaves", "missing", "update",
    ];

    /// <summary>Plus long préfixe commun (insensible à la casse) d'un ensemble de chaînes.</summary>
    public static string CommonPrefix(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        var prefix = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            var candidate = values[i];
            var len = System.Math.Min(prefix.Length, candidate.Length);
            var j = 0;
            while (j < len && char.ToLowerInvariant(prefix[j]) == char.ToLowerInvariant(candidate[j]))
            {
                j++;
            }

            prefix = prefix[..j];
            if (prefix.Length == 0)
            {
                break;
            }
        }

        return prefix;
    }
}
