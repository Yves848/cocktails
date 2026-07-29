using Cocktails.Core.Process;

namespace Cocktails.Core;

/// <summary>
/// Levée lorsqu'une commande <c>brew</c> se termine avec un code d'erreur non nul.
/// </summary>
public sealed class HomebrewException : Exception
{
    public int ExitCode { get; }
    public string StandardError { get; }

    public HomebrewException(string command, ProcessResult result)
        : base($"La commande « brew {command} » a échoué (code {result.ExitCode}).")
    {
        ExitCode = result.ExitCode;
        StandardError = result.StandardError;
    }
}
