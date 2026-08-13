namespace Cocktails.Core.Process;

/// <summary>Résultat d'exécution d'un processus externe.</summary>
/// <param name="ExitCode">Code de sortie du processus.</param>
/// <param name="StandardOutput">Sortie standard complète.</param>
/// <param name="StandardError">Sortie d'erreur complète.</param>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>Vrai si le processus s'est terminé avec un code 0.</summary>
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Abstraction de l'exécution d'un processus externe. Permet d'isoler la couche
/// Homebrew de <see cref="System.Diagnostics.Process"/> et de la tester sans lancer
/// réellement <c>brew</c>.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Exécute <paramref name="fileName"/> avec ses arguments et attend la fin.
    /// Si <paramref name="output"/> est fourni, chaque ligne de stdout/stderr lui est
    /// signalée au fil de l'eau (en plus d'être accumulée dans le résultat final).
    /// <paramref name="environment"/> ajoute des variables à celles héritées du processus.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null);
}
