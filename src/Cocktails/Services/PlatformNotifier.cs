using System;
using Cocktails.Core.Process;

namespace Cocktails.Services;

/// <summary>Choisit l'implémentation de notification adaptée au contexte d'exécution.</summary>
public static class PlatformNotifier
{
    public static INotifier Create(IProcessRunner runner)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return new NullNotifier();
        }

        // UNUserNotificationCenter exige un bundle .app (avec bundle id). En bundle, on
        // obtient l'attribution native « Cocktails » ; sinon (dev), repli sur osascript.
        var path = Environment.ProcessPath ?? string.Empty;
        return path.Contains("/Contents/MacOS/", StringComparison.Ordinal)
            ? new MacUserNotifier()
            : new MacNotifier(runner);
    }
}
