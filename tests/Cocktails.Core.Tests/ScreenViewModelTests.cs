using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

public class ScreenViewModelTests
{
    /// <summary>Faux service enregistrant les appels et pilotant l'état « installés ».</summary>
    private sealed class FakeHomebrewService : IHomebrewService
    {
        public List<string> Installed { get; init; } = [];
        public List<string> Casks { get; init; } = [];
        public List<string> InstallCalls { get; } = [];
        public List<string> UninstallCalls { get; } = [];

        public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<Package>();
            list.AddRange(Installed.Select(n => new Package(n, PackageKind.Formula, InstalledVersion: "1.0")));
            list.AddRange(Casks.Select(n => new Package(n, PackageKind.Cask, InstalledVersion: "1.0")));
            return Task.FromResult<IReadOnlyList<Package>>(list);
        }

        public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([new Package(query, PackageKind.Formula)]);

        public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(new PackageDetails(
                name, PackageKind.Formula, "desc", "https://example.org",
                "1.0", "1.0", ["dep1", "dep2"], false, "homebrew/core"));

        public Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            InstallCalls.Add(name);
            Installed.Add(name);
            return Task.CompletedTask;
        }

        public Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            UninstallCalls.Add(name);
            Installed.Remove(name);
            return Task.CompletedTask;
        }

        public Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public List<string> Maintenance { get; } = [];

        public Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("cleanup");
            return Task.CompletedTask;
        }

        public Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("autoremove");
            return Task.CompletedTask;
        }

        public Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("doctor");
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHomebrewService : IHomebrewService
    {
        public Task<IReadOnlyList<Package>> GetInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<IReadOnlyList<Package>> GetOutdatedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Package>>([]);

        public Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(new PackageDetails(name, PackageKind.Formula, null, null, null, null, [], false, null));

        public Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => throw new HomebrewException(
                "install " + name,
                new Cocktails.Core.Process.ProcessResult(1, "", "No available formula"));

        public Task UninstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpgradeAsync(string? name = null, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CleanupAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AutoremoveAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DoctorAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task Installed_Activate_LoadsInstalledOnce()
    {
        var fake = new FakeHomebrewService { Installed = { "git", "wget" } };
        var vm = new InstalledViewModel(fake);

        await vm.ActivateAsync();
        Assert.Equal(2, vm.Packages.Count);

        // Deuxième activation : pas de rechargement (chargement paresseux unique).
        fake.Installed.Add("node");
        await vm.ActivateAsync();
        Assert.Equal(2, vm.Packages.Count);
    }

    [Fact]
    public async Task Installed_Uninstall_ConfirmThenCallsServiceAndReloads()
    {
        var fake = new FakeHomebrewService { Installed = { "git", "wget" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        // 1er clic : demande confirmation, rien n'est fait.
        vm.UninstallCommand.Execute(new Package("git", PackageKind.Formula, InstalledVersion: "1.0"));
        Assert.NotNull(vm.Confirmation);
        Assert.Empty(fake.UninstallCalls);

        // Confirmation : désinstallation réelle + rechargement.
        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Null(vm.Confirmation);
        Assert.Equal(["git"], fake.UninstallCalls);
        Assert.DoesNotContain(vm.Packages, p => p.Name == "git");
        Assert.Contains(vm.Packages, p => p.Name == "wget");
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Installed_Uninstall_SkipsConfirmationWhenSettingOff()
    {
        var settings = new AppSettings { ConfirmBeforeUninstall = false };
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake, settings);
        await vm.ActivateAsync();

        vm.UninstallCommand.Execute(vm.Packages[0]);

        Assert.Null(vm.Confirmation);               // pas de dialogue
        Assert.Equal(["git"], fake.UninstallCalls); // désinstallé directement
    }

    [Fact]
    public async Task Installed_Uninstall_Cancel_DoesNothing()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        vm.UninstallCommand.Execute(vm.Packages[0]);
        vm.CancelConfirmationCommand.Execute(null);

        Assert.Null(vm.Confirmation);
        Assert.Empty(fake.UninstallCalls);
        Assert.Contains(vm.Packages, p => p.Name == "git");
    }

    [Fact]
    public async Task Installed_SelectingPackage_LoadsDetails()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        vm.SelectedPackage = vm.Packages[0];

        Assert.NotNull(vm.Details);
        Assert.Equal("git", vm.Details!.Name);
        Assert.Equal(2, vm.Details.Dependencies.Count);
        Assert.False(vm.IsLoadingDetails);
    }

    [Fact]
    public async Task Installed_Filter_NarrowsAndListIsSorted()
    {
        var fake = new FakeHomebrewService { Installed = { "wget", "git", "gnupg" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        // Tri alphabétique appliqué à l'affichage.
        Assert.Equal(["git", "gnupg", "wget"], vm.Packages.Select(p => p.Name));

        vm.FilterText = "gn";
        Assert.Equal(["gnupg"], vm.Packages.Select(p => p.Name));

        vm.FilterText = "";
        Assert.Equal(3, vm.Packages.Count);
    }

    [Fact]
    public async Task Installed_KindFilter_ShowsOnlySelectedType()
    {
        var fake = new FakeHomebrewService { Installed = { "git" }, Casks = { "firefox" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();
        Assert.Equal(2, vm.Packages.Count);

        vm.SetKindFilterCommand.Execute(PackageKindFilter.Cask);
        Assert.Equal(["firefox"], vm.Packages.Select(p => p.Name));
        Assert.True(vm.IsKindCask);

        vm.SetKindFilterCommand.Execute(PackageKindFilter.All);
        Assert.Equal(2, vm.Packages.Count);
    }

    [Fact]
    public async Task Search_MarksAlreadyInstalledResults()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new SearchViewModel(fake);
        vm.SearchQuery = "git";

        await vm.SearchCommand.ExecuteAsync(null);

        var result = Assert.Single(vm.Packages);
        Assert.True(result.IsInstalled);
        Assert.Contains("déjà installé", vm.StatusMessage);
    }

    [Fact]
    public async Task Installed_ClearingSelection_ClearsDetails()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();
        vm.SelectedPackage = vm.Packages[0];

        vm.SelectedPackage = null;

        Assert.Null(vm.Details);
    }

    [Fact]
    public async Task Search_SelectingResult_LoadsDetails()
    {
        var vm = new SearchViewModel(new FakeHomebrewService());
        vm.SearchQuery = "ripgrep";
        await vm.SearchCommand.ExecuteAsync(null);

        vm.SelectedPackage = vm.Packages[0];

        Assert.NotNull(vm.Details);
        Assert.Equal("ripgrep", vm.Details!.Name);
        Assert.False(vm.IsLoadingDetails);
    }

    [Fact]
    public async Task Search_Install_CallsService()
    {
        var fake = new FakeHomebrewService();
        var vm = new SearchViewModel(fake);

        await vm.InstallCommand.ExecuteAsync(new Package("ripgrep", PackageKind.Formula));

        Assert.Equal(["ripgrep"], fake.InstallCalls);
        Assert.Contains("ripgrep", vm.StatusMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Search_Install_SurfacesBrewError()
    {
        var vm = new SearchViewModel(new ThrowingHomebrewService());

        await vm.InstallCommand.ExecuteAsync(new Package("bogus", PackageKind.Formula));

        Assert.Contains("Erreur", vm.StatusMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Maintenance_CleanupAndDoctor_CallService()
    {
        var fake = new FakeHomebrewService();
        var vm = new MaintenanceViewModel(fake);

        await vm.CleanupCommand.ExecuteAsync(null);
        await vm.DoctorCommand.ExecuteAsync(null);

        Assert.Equal(["cleanup", "doctor"], fake.Maintenance);
    }

    [Fact]
    public async Task Maintenance_Autoremove_ConfirmsFirst()
    {
        var fake = new FakeHomebrewService();
        var vm = new MaintenanceViewModel(fake);

        vm.AutoremoveCommand.Execute(null);
        Assert.NotNull(vm.Confirmation);
        Assert.Empty(fake.Maintenance);

        await vm.ConfirmCommand.ExecuteAsync(null);
        Assert.Equal(["autoremove"], fake.Maintenance);
    }

    [Fact]
    public void Shell_DefaultsToInstalledScreen()
    {
        var vm = new MainViewModel(new FakeHomebrewService());

        Assert.Equal(5, vm.NavItems.Count);
        Assert.Equal("Installés", vm.SelectedNav?.Title);
        Assert.IsType<InstalledViewModel>(vm.CurrentScreen);
    }
}
