namespace Cocktails.Core.Models;

/// <summary>Un dépôt de formules/casks (<c>brew tap</c>).</summary>
/// <param name="Name">Nom complet (ex. <c>homebrew/core</c>, <c>felixkratz/formulae</c>).</param>
/// <param name="Official">Vrai si c'est un tap officiel Homebrew.</param>
/// <param name="FormulaCount">Nombre de formulae fournies.</param>
/// <param name="CaskCount">Nombre de casks fournis.</param>
/// <param name="CustomRemote">Vrai si le dépôt distant est personnalisé (URL explicite).</param>
public record BrewTap(string Name, bool Official, int FormulaCount, int CaskCount, bool CustomRemote)
{
    /// <summary>Libellé de la nature du tap.</summary>
    public string KindLabel => Official ? "officiel" : "tiers";

    /// <summary>Résumé du contenu (formulae · casks).</summary>
    public string Summary => $"{FormulaCount} formula(e) · {CaskCount} cask(s)";
}
