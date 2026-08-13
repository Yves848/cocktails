using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    // Sous-commandes dont les arguments sont des paquets DÉJÀ installés (complétion ciblée).
    private static readonly HashSet<string> InstalledOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "uninstall", "remove", "rm", "reinstall", "upgrade", "pin", "unpin", "link", "unlink", "uses",
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
    /// Vrai si les arguments de cette sous-commande sont des paquets déjà installés
    /// (uninstall, reinstall, pin…) → complétion contre la liste des installés.
    /// </summary>
    public static bool CompletesInstalledOnly(string subcommand)
        => subcommand.Length > 0 && InstalledOnly.Contains(subcommand);

    /// <summary>
    /// Sous-commandes brew usuelles proposées à la complétion du premier mot.
    /// </summary>
    public static readonly string[] Subcommands =
    [
        "install", "uninstall", "reinstall", "upgrade", "info", "search", "list", "deps",
        "uses", "pin", "unpin", "tap", "untap", "outdated", "cleanup", "autoremove", "doctor",
        "services", "link", "unlink", "home", "desc", "leaves", "missing", "update",
    ];

    // Options communes à la plupart des sous-commandes.
    private static readonly string[] CommonOptions = ["--help", "--verbose", "--debug", "--quiet"];

    // Options spécifiques par sous-commande (complétion des flags « --… »).
    private static readonly Dictionary<string, string[]> OptionsBySub = new(StringComparer.OrdinalIgnoreCase)
    {
        ["install"] = ["--cask", "--formula", "--force", "--HEAD", "--build-from-source", "--no-quarantine", "--dry-run"],
        ["reinstall"] = ["--cask", "--formula", "--force", "--no-quarantine"],
        ["uninstall"] = ["--cask", "--formula", "--force", "--zap", "--ignore-dependencies"],
        ["upgrade"] = ["--cask", "--formula", "--greedy", "--dry-run", "--force"],
        ["list"] = ["--versions", "--cask", "--formula", "--pinned", "--full-name", "-1"],
        ["outdated"] = ["--cask", "--formula", "--greedy", "--verbose", "--json"],
        ["info"] = ["--json", "--cask", "--formula", "--github"],
        ["deps"] = ["--tree", "--installed", "--cask", "--formula", "--include-build"],
        ["uses"] = ["--installed", "--recursive", "--cask", "--formula"],
        ["search"] = ["--cask", "--formula", "--desc"],
        ["cleanup"] = ["--prune", "--dry-run", "-s"],
        ["services"] = ["--all"],
    };

    /// <summary>Options (flags <c>--…</c>) proposées pour une sous-commande donnée.</summary>
    public static string[] OptionsFor(string subcommand)
        => OptionsBySub.TryGetValue(subcommand, out var specific)
            ? [.. specific, .. CommonOptions]
            : CommonOptions;

    /// <summary>
    /// Repère dans une sortie brew les commandes proposées : lignes commençant par
    /// <c>brew </c> (ex. « brew install --cask X ») ou entre backticks (« Try `brew …` »).
    /// Dédoublonne en conservant l'ordre.
    /// </summary>
    public static IReadOnlyList<string> SuggestedCommands(IEnumerable<string> outputLines)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in outputLines)
        {
            var line = raw.Trim();
            string? command = null;
            if (line.StartsWith("brew ", StringComparison.Ordinal))
            {
                command = line;
            }
            else
            {
                var match = Regex.Match(line, "`(brew [^`]+)`");
                if (match.Success)
                {
                    command = match.Groups[1].Value.Trim();
                }
            }

            if (command is not null && seen.Add(command))
            {
                result.Add(command);
            }
        }

        return result;
    }

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
