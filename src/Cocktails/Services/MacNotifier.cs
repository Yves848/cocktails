using System.Threading.Tasks;
using Cocktails.Core.Process;

namespace Cocktails.Services;

/// <summary>
/// Notification système macOS via <c>osascript</c> (« display notification »).
/// Sans dépendance externe — cohérent avec le reste qui pilote déjà des processus.
/// </summary>
public sealed class MacNotifier : INotifier
{
    private readonly IProcessRunner _runner;

    public MacNotifier(IProcessRunner runner) => _runner = runner;

    public async Task NotifyAsync(string title, string message)
    {
        var script = $"display notification \"{Escape(message)}\" with title \"{Escape(title)}\"";
        try
        {
            await _runner.RunAsync("/usr/bin/osascript", ["-e", script]).ConfigureAwait(false);
        }
        catch (System.Exception)
        {
            // Une notification qui échoue ne doit jamais casser le monitoring.
        }
    }

    // Échappe pour une chaîne AppleScript (antislash puis guillemet double).
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
