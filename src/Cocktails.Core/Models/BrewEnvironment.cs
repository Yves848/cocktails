namespace Cocktails.Core.Models;

/// <summary>Environnement Homebrew (issu de <c>brew config</c> / <c>brew --cache</c>).</summary>
/// <param name="Version">Version de Homebrew.</param>
/// <param name="Prefix">Préfixe d'installation (<c>HOMEBREW_PREFIX</c>).</param>
/// <param name="Cache">Répertoire de cache.</param>
public record BrewEnvironment(string Version, string Prefix, string Cache);
