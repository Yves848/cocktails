using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.Core.Process;

namespace Cocktails.Core.Tests;

public class HomebrewStreamingTests
{
    /// <summary>Runner scripté : signale des lignes prédéfinies puis renvoie un code.</summary>
    private sealed class ScriptedRunner : IProcessRunner
    {
        private readonly string[] _lines;
        private readonly int _exitCode;

        public List<string> LastArguments { get; } = [];

        public ScriptedRunner(string[] lines, int exitCode)
        {
            _lines = lines;
            _exitCode = exitCode;
        }

        public Task<ProcessResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            IProgress<string>? output = null,
            CancellationToken cancellationToken = default,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            LastArguments.Clear();
            LastArguments.AddRange(arguments);
            foreach (var line in _lines)
            {
                output?.Report(line);
            }

            return Task.FromResult(new ProcessResult(_exitCode, string.Join('\n', _lines), string.Empty));
        }
    }

    /// <summary>Collecteur synchrone thread-safe (les deux pompes stdout/stderr concourent).</summary>
    private sealed class CollectingProgress : IProgress<string>
    {
        private readonly object _gate = new();
        public List<string> Lines { get; } = [];

        public void Report(string value)
        {
            lock (_gate)
            {
                Lines.Add(value);
            }
        }
    }

    [Fact]
    public async Task InstallAsync_StreamsOutputLinesInOrder()
    {
        var runner = new ScriptedRunner(["==> Fetching wget", "==> Pouring wget", "🍺  poured"], 0);
        var service = new HomebrewService(runner, "/opt/homebrew/bin/brew");
        var progress = new CollectingProgress();

        await service.InstallAsync("wget", PackageKind.Formula, progress);

        Assert.Equal(["==> Fetching wget", "==> Pouring wget", "🍺  poured"], progress.Lines);
        Assert.Equal(["install", "wget"], runner.LastArguments);
    }

    [Fact]
    public async Task InstallAsync_Cask_AddsCaskFlag()
    {
        var runner = new ScriptedRunner([], 0);
        var service = new HomebrewService(runner, "/opt/homebrew/bin/brew");

        await service.InstallAsync("firefox", PackageKind.Cask);

        Assert.Equal(["install", "--cask", "firefox"], runner.LastArguments);
    }

    [Fact]
    public async Task UpgradeAsync_All_UsesBareUpgradeAndStreams()
    {
        var runner = new ScriptedRunner(["==> Upgrading 2 outdated packages"], 0);
        var service = new HomebrewService(runner, "/opt/homebrew/bin/brew");
        var progress = new CollectingProgress();

        await service.UpgradeAsync(null, progress);

        Assert.Equal(["upgrade"], runner.LastArguments);
        Assert.Single(progress.Lines);
    }

    [Fact]
    public async Task FailingCommand_ThrowsHomebrewException()
    {
        var runner = new ScriptedRunner(["Error: No such keg"], 1);
        var service = new HomebrewService(runner, "/opt/homebrew/bin/brew");

        await Assert.ThrowsAsync<HomebrewException>(() => service.UninstallAsync("ghost"));
    }

    [Fact]
    public async Task ProcessRunner_StreamsRealSubprocessLinesFromStdoutAndStderr()
    {
        var runner = new ProcessRunner();
        var progress = new CollectingProgress();

        var result = await runner.RunAsync(
            "/bin/sh",
            ["-c", "printf 'line1\\nline2\\n'; printf 'boom\\n' 1>&2"],
            progress);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("line1", progress.Lines);
        Assert.Contains("line2", progress.Lines);
        Assert.Contains("boom", progress.Lines);           // stderr aussi diffusé
        Assert.Contains("line1", result.StandardOutput);   // et accumulé
        Assert.Contains("boom", result.StandardError);
    }
}
