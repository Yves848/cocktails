namespace Cocktails.Core.Models;

/// <summary>
/// Un nœud de l'arbre de dépendances (<c>brew deps --tree &lt;name&gt;</c>). La profondeur 0
/// est le paquet lui-même (racine) ; les dépendances transitives ont une profondeur croissante.
/// </summary>
/// <param name="Depth">Niveau dans l'arbre (0 = racine).</param>
/// <param name="Name">Nom de la formule.</param>
public record DependencyNode(int Depth, string Name)
{
    /// <summary>Vrai pour le paquet racine (le paquet consulté).</summary>
    public bool IsRoot => Depth == 0;
}
