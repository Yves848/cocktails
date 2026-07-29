using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

public class MainViewModelTests
{
    /// <summary>
    /// Faux service enregistrant les appels et renvoyant un jeu d'« installés »
    /// contrôlable, pour vérifier le câblage des commandes du view model.
    /// </summary>
    private sealed class FakeHomebrewService : IHomebrewService
    {
        public List<string> Installed { get; } = [];
        public List<string> InstallCalls { get; } = [];
        public List<string> UninstallCalls { get; } = [];

        public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Package> list = Installed
                .ConvertAll(n => new Package(n, PackageKind.Formula, InstalledVersion: "1.0"));
            return Task.FromResult(list);
        }

        public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([new Package(query, PackageKind.Formula)]);

        public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task InstallAsync(string name, CancellationToken cancellationToken = default)
        {
            InstallCalls.Add(name);
            Installed.Add(name);
            return Task.CompletedTask;
        }

        public Task UninstallAsync(string name, CancellationToken cancellationToken = default)
        {
            UninstallCalls.Add(name);
            Installed.Remove(name);
            return Task.CompletedTask;
        }

        public Task UpgradeAsync(string? name = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task InstallCommand_CallsServiceAndRefreshesCurrentView()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new MainViewModel(fake);
        await vm.RefreshInstalledCommand.ExecuteAsync(null);

        var wget = new Package("wget", PackageKind.Formula);
        await vm.InstallCommand.ExecuteAsync(wget);

        Assert.Equal(["wget"], fake.InstallCalls);
        // La vue « installés » a été rechargée : wget y figure désormais.
        Assert.Contains(vm.Packages, p => p.Name == "wget");
        Assert.False(vm.IsBusy);
        Assert.Contains("wget", vm.StatusMessage);
    }

    [Fact]
    public async Task UninstallCommand_CallsServiceAndDropsPackageFromView()
    {
        var fake = new FakeHomebrewService { Installed = { "git", "wget" } };
        var vm = new MainViewModel(fake);
        await vm.RefreshInstalledCommand.ExecuteAsync(null);

        var git = new Package("git", PackageKind.Formula, InstalledVersion: "1.0");
        await vm.UninstallCommand.ExecuteAsync(git);

        Assert.Equal(["git"], fake.UninstallCalls);
        Assert.DoesNotContain(vm.Packages, p => p.Name == "git");
        Assert.Contains(vm.Packages, p => p.Name == "wget");
    }

    [Fact]
    public async Task InstallCommand_SurfacesBrewErrorInStatus()
    {
        var vm = new MainViewModel(new ThrowingHomebrewService());

        await vm.InstallCommand.ExecuteAsync(new Package("bogus", PackageKind.Formula));

        Assert.Contains("Erreur", vm.StatusMessage);
        Assert.False(vm.IsBusy);
    }

    private sealed class ThrowingHomebrewService : IHomebrewService
    {
        public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task InstallAsync(string name, CancellationToken cancellationToken = default)
            => throw new HomebrewException(
                "install " + name,
                new Cocktails.Core.Process.ProcessResult(1, "", "No available formula"));

        public Task UninstallAsync(string name, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpgradeAsync(string? name = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
