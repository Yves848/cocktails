using System.Diagnostics;
using System.IO;
using System.Text;

namespace Cocktails.Core.Process;

/// <summary>
/// Implémentation de <see cref="IProcessRunner"/> basée sur
/// <see cref="System.Diagnostics.Process"/>. Lit stdout et stderr ligne à ligne, en
/// parallèle (pour éviter les interblocages sur les grosses sorties) : chaque ligne est
/// signalée à <c>output</c> au fil de l'eau et accumulée pour le résultat final.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = PumpAsync(process.StandardOutput, output, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, output, cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    /// <summary>Lit un flux ligne à ligne : accumule le texte et signale chaque ligne.</summary>
    private static async Task<string> PumpAsync(
        StreamReader reader, IProgress<string>? output, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            builder.Append(line).Append('\n');
            output?.Report(line);
        }

        return builder.ToString();
    }
}
