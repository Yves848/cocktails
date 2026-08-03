using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.Services;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

public class UpdateMonitorTests
{
    /// <summary>Service dont on contrôle uniquement la liste des obsolètes.</summary>
    private sealed class OutdatedStub : IHomebrewService
    {
        public IReadOnlyList<Package> Outdated { get; set; } = [];

        public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Outdated);

        public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> GetInfoForAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(new PackageDetails(name, PackageKind.Formula, null, null, null, null, [], false, null));

        public Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnpinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetDependentsAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyList<string>> GetLeavesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<BrewEnvironment> GetEnvironmentAsync(CancellationToken cancellationToken = default) => Task.FromResult(new BrewEnvironment("?", "/opt/homebrew", "/cache"));
        public Task<bool> GetAnalyticsEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task SetAnalyticsAsync(bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReinstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<MissingDependency>> GetMissingAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MissingDependency>>([]);
        public Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BrewService>>([]);
        public Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BrewTap>> GetTapsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BrewTap>>([]);
        public Task AddTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task TrustTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingNotifier : INotifier
    {
        public List<string> Messages { get; } = [];
        public Task NotifyAsync(string title, string message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private static Package Pkg(string name) => new(name, PackageKind.Formula);

    [Fact]
    public async Task Check_SetsOutdatedCount()
    {
        var stub = new OutdatedStub { Outdated = [Pkg("git"), Pkg("node")] };
        var monitor = new UpdateMonitor(stub, new AppSettings(), new RecordingNotifier());

        await monitor.CheckNowAsync();

        Assert.Equal(2, monitor.OutdatedCount);
    }

    [Fact]
    public async Task Check_NotifiesOnlyOnNewlyOutdated_AfterFirstPass()
    {
        var stub = new OutdatedStub { Outdated = [Pkg("git")] };
        var notifier = new RecordingNotifier();
        var monitor = new UpdateMonitor(stub, new AppSettings { NotificationsEnabled = true }, notifier);

        await monitor.CheckNowAsync();                       // état initial → pas de notif
        Assert.Empty(notifier.Messages);

        stub.Outdated = [Pkg("git"), Pkg("node")];
        await monitor.CheckNowAsync();                       // « node » nouveau → notif
        Assert.Single(notifier.Messages);
        Assert.Contains("node", notifier.Messages[0]);

        await monitor.CheckNowAsync();                       // rien de nouveau → pas de notif
        Assert.Single(notifier.Messages);
    }

    [Fact]
    public async Task Check_RaisesOutdatedChanged_OnlyWhenSetChanges()
    {
        var stub = new OutdatedStub { Outdated = [Pkg("git")] };
        var monitor = new UpdateMonitor(stub, new AppSettings { NotificationsEnabled = false }, new RecordingNotifier());
        var changes = 0;
        monitor.OutdatedChanged += (_, _) => changes++;

        await monitor.CheckNowAsync();      // premier passage → pas d'événement
        Assert.Equal(0, changes);

        stub.Outdated = [Pkg("git"), Pkg("node")];
        await monitor.CheckNowAsync();      // ensemble modifié → un événement
        Assert.Equal(1, changes);

        await monitor.CheckNowAsync();      // ensemble identique → aucun événement
        Assert.Equal(1, changes);

        stub.Outdated = [Pkg("git")];
        await monitor.CheckNowAsync();      // « node » à jour → nouvel événement
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task Check_DoesNotNotify_WhenNotificationsDisabled()
    {
        var stub = new OutdatedStub { Outdated = [Pkg("git")] };
        var notifier = new RecordingNotifier();
        var monitor = new UpdateMonitor(stub, new AppSettings { NotificationsEnabled = false }, notifier);

        await monitor.CheckNowAsync();
        stub.Outdated = [Pkg("git"), Pkg("node")];
        await monitor.CheckNowAsync();

        Assert.Empty(notifier.Messages);
        Assert.Equal(2, monitor.OutdatedCount);
    }
}
