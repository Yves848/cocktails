namespace Cocktails.Core.Models;

/// <summary>Un dépôt de formules/casks (<c>brew tap</c>).</summary>
/// <param name="Name">Nom complet (ex. <c>homebrew/core</c>, <c>felixkratz/formulae</c>).</param>
/// <param name="Official">Vrai si c'est un tap officiel Homebrew.</param>
/// <param name="FormulaCount">Nombre de formulae fournies.</param>
/// <param name="CaskCount">Nombre de casks fournis.</param>
/// <param name="CustomRemote">Vrai si le dépôt distant est personnalisé (URL explicite).</param>
/// <param name="Trusted">Vrai si Homebrew est autorisé à charger ce tap (<c>brew trust</c>).
/// Les taps officiels sont toujours de confiance.</param>
public record BrewTap(
    string Name, bool Official, int FormulaCount, int CaskCount, bool CustomRemote, bool Trusted = false)
{
    /// <summary>Libellé de la nature du tap.</summary>
    public string KindLabel => Official ? "officiel" : "tiers";

    /// <summary>Résumé du contenu (formulae · casks).</summary>
    public string Summary => $"{FormulaCount} formula(e) · {CaskCount} cask(s)";

    /// <summary>Vrai si l'action « faire confiance » a du sens (tiers pas encore approuvé).</summary>
    public bool CanTrust => !Official && !Trusted;
}
