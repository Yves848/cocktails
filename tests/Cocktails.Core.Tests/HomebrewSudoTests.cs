using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.Core.Process;
using Cocktails.Core.Sudo;

namespace Cocktails.Core.Tests;

/// <summary>
/// Vérifie que <c>SUDO_ASKPASS</c> est exporté aux commandes brew susceptibles d'appeler
/// <c>sudo</c> (installeurs de casks), et à elles seules.
/// </summary>
public class HomebrewSudoTests
{
    private sealed class CapturingRunner : IProcessRunner
    {
        public IReadOnlyDictionary<string, string>? LastEnvironment { get; private set; }

        public Task<ProcessResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            IProgress<string>? output = null,
            CancellationToken cancellationToken = default,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            LastEnvironment = environment;
            return Task.FromResult(new ProcessResult(0, "{}", string.Empty));
        }
    }

    private sealed class FakeAskpass : ISudoAskpass
    {
        public string? HelperPath { get; set; } = "/tmp/cocktails/askpass.sh";
        public int OperationsStarted { get; private set; }

        public void BeginOperation() => OperationsStarted++;
    }

    [Fact]
    public async Task InstallCask_ExportsSudoAskpassHelper()
    {
        var runner = new CapturingRunner();
        var askpass = new FakeAskpass();
        var service = new HomebrewService(runner, askpass: askpass);

        await service.InstallAsync("dotnet-sdk", PackageKind.Cask);

        Assert.NotNull(runner.LastEnvironment);
        Assert.Equal("/tmp/cocktails/askpass.sh", runner.LastEnvironment!["SUDO_ASKPASS"]);
    }

    [Fact]
    public async Task MutatingCommand_StartsANewAskpassOperation()
    {
        var askpass = new FakeAskpass();
        var service = new HomebrewService(new CapturingRunner(), askpass: askpass);

        await service.InstallAsync("dotnet-sdk", PackageKind.Cask);
        await service.UninstallAsync("dotnet-sdk");

        Assert.Equal(2, askpass.OperationsStarted);
    }

    [Fact]
    public async Task TerminalCommand_ExportsSudoAskpassHelper()
    {
        var runner = new CapturingRunner();
        var service = new HomebrewService(runner, askpass: new FakeAskpass());

        await service.RunBrewAsync(["upgrade", "dotnet-sdk"]);

        Assert.Equal("/tmp/cocktails/askpass.sh", runner.LastEnvironment?["SUDO_ASKPASS"]);
    }

    [Fact]
    public async Task ReadOnlyCommand_DoesNotExportSudoAskpass()
    {
        var runner = new CapturingRunner();
        var service = new HomebrewService(runner, askpass: new FakeAskpass());

        await service.GetOutdatedAsync();

        Assert.Null(runner.LastEnvironment);
    }

    [Fact]
    public async Task UnavailableHelper_ExportsNothing()
    {
        var runner = new CapturingRunner();
        var service = new HomebrewService(runner, askpass: new FakeAskpass { HelperPath = null });

        await service.InstallAsync("dotnet-sdk", PackageKind.Cask);

        Assert.Null(runner.LastEnvironment);
    }
}
