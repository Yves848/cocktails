using System.Collections.Generic;

namespace Cocktails.Core.Models;

/// <summary>
/// Catalogue Homebrew : tous les noms connus, séparés en formulae et casks. Sert à la
/// complétion du terminal et à la détection automatique des casks (ajout de <c>--cask</c>).
/// </summary>
/// <param name="Formulae">Noms de toutes les formulae (<c>brew formulae</c>).</param>
/// <param name="Casks">Noms de tous les casks (<c>brew casks</c>).</param>
public record BrewCatalog(IReadOnlyList<string> Formulae, IReadOnlyList<string> Casks);
