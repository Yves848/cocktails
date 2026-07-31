namespace Cocktails.Core.Models;

/// <summary>
/// Une formule installée à laquelle il manque des dépendances (<c>brew missing</c>).
/// </summary>
/// <param name="Formula">Formule installée concernée.</param>
/// <param name="Missing">Dépendances manquantes (non installées).</param>
public record MissingDependency(string Formula, IReadOnlyList<string> Missing);
