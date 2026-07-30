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

        public HashSet<string> Pinned { get; } = [];

        public Task<PackageDetails> GetInfoAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(new PackageDetails(
                name, PackageKind.Formula, "desc", "https://example.org",
                "1.0", "1.0", ["dep1", "dep2"], Pinned.Contains(name), "homebrew/core"));

        public Task PinAsync(string name, CancellationToken cancellationToken = default)
        {
            Pinned.Add(name);
            return Task.CompletedTask;
        }

        public Task UnpinAsync(string name, CancellationToken cancellationToken = default)
        {
            Pinned.Remove(name);
            return Task.CompletedTask;
        }

        public List<string> Dependents { get; init; } = [];
        public List<string> Leaves { get; init; } = [];

        public Task<IReadOnlyList<string>> GetDependentsAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Dependents);

        public Task<IReadOnlyList<string>> GetLeavesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Leaves);

        public Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("update");
            return Task.CompletedTask;
        }

        public Task InstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            InstallCalls.Add(name);
            Installed.Add(name);
            return Task.CompletedTask;
        }

        public Task ReinstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("reinstall:" + name);
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

        public Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("dump:" + path);
            return Task.CompletedTask;
        }

        public Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("bundle-install:" + path);
            return Task.CompletedTask;
        }

        public List<BrewService> ServicesList { get; init; } = [];

        public Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BrewService>>(ServicesList);

        public Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("start:" + name);
            return Task.CompletedTask;
        }

        public Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("stop:" + name);
            return Task.CompletedTask;
        }

        public Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("restart:" + name);
            return Task.CompletedTask;
        }

        public List<BrewTap> TapsList { get; init; } = [];

        public Task<IReadOnlyList<BrewTap>> GetTapsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BrewTap>>(TapsList);

        public Task AddTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("tap:" + name);
            TapsList.Add(new BrewTap(name, false, 0, 0, false));
            return Task.CompletedTask;
        }

        public Task RemoveTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("untap:" + name);
            TapsList.RemoveAll(t => t.Name == name);
            return Task.CompletedTask;
        }

        public Task TrustTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
        {
            Maintenance.Add("trust:" + name);
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

        public Task PinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnpinAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetDependentsAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyList<string>> GetLeavesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task UpdateIndexAsync(IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ReinstallAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

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

        public Task BundleDumpAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task BundleInstallAsync(string path, IProgress<string>? output = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BrewService>> GetServicesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BrewService>>([]);

        public Task StartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestartServiceAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BrewTap>> GetTapsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BrewTap>>([]);
        public Task AddTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task TrustTapAsync(string name, IProgress<string>? output = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
    public async Task Installed_Reinstall_CallsService()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        await vm.ReinstallCommand.ExecuteAsync(vm.Packages[0]);

        Assert.Contains("reinstall:git", fake.Maintenance);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Installed_Pin_CallsServiceAndReloadsDetail()
    {
        var fake = new FakeHomebrewService { Installed = { "git" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();
        vm.SelectedPackage = vm.Packages[0];
        Assert.False(vm.Details!.IsPinned);

        await vm.PinCommand.ExecuteAsync(vm.SelectedPackage);

        Assert.Contains("git", fake.Pinned);
        Assert.True(vm.Details!.IsPinned);   // détail rechargé

        await vm.UnpinCommand.ExecuteAsync(vm.SelectedPackage);
        Assert.DoesNotContain("git", fake.Pinned);
        Assert.False(vm.Details!.IsPinned);
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
    public async Task Installed_Selecting_LoadsDependents()
    {
        var fake = new FakeHomebrewService { Installed = { "openssl@3" }, Dependents = { "bat", "node" } };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();

        vm.SelectedPackage = vm.Packages[0];

        Assert.Equal(["bat", "node"], vm.Dependents);
    }

    [Fact]
    public async Task Installed_LeavesOnly_FiltersToLeaves()
    {
        var fake = new FakeHomebrewService
        {
            Installed = { "git", "openssl@3", "pcre2" },
            Leaves = { "git" },   // seul git est une racine
        };
        var vm = new InstalledViewModel(fake);
        await vm.ActivateAsync();
        Assert.Equal(3, vm.Packages.Count);

        vm.LeavesOnly = true;
        Assert.Equal(["git"], vm.Packages.Select(p => p.Name));

        vm.LeavesOnly = false;
        Assert.Equal(3, vm.Packages.Count);
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
    public async Task Outdated_UpdateIndex_CallsUpdateThenReloads()
    {
        var fake = new FakeHomebrewService();
        var vm = new OutdatedViewModel(fake);

        await vm.UpdateIndexCommand.ExecuteAsync(null);

        Assert.Contains("update", fake.Maintenance);
        Assert.False(vm.IsBusy);
        Assert.Contains("Index à jour", vm.StatusMessage);
    }

    [Fact]
    public async Task Maintenance_ExportBrewfile_CallsDump()
    {
        var fake = new FakeHomebrewService();
        var vm = new MaintenanceViewModel(fake);

        await vm.ExportBrewfileAsync("/tmp/Brewfile");

        Assert.Contains("dump:/tmp/Brewfile", fake.Maintenance);
        Assert.Contains("exporté", vm.StatusMessage);
    }

    [Fact]
    public async Task Maintenance_ImportBrewfile_ConfirmsThenInstalls()
    {
        var fake = new FakeHomebrewService();
        var vm = new MaintenanceViewModel(fake);

        vm.ImportBrewfile("/tmp/Brewfile");
        Assert.NotNull(vm.Confirmation);
        Assert.Empty(fake.Maintenance);

        await vm.ConfirmCommand.ExecuteAsync(null);
        Assert.Contains("bundle-install:/tmp/Brewfile", fake.Maintenance);
    }

    [Fact]
    public async Task Services_Activate_LoadsList()
    {
        var fake = new FakeHomebrewService
        {
            ServicesList = { new BrewService("redis", "started", "yves", "/x/redis.plist") },
        };
        var vm = new ServicesViewModel(fake);

        await vm.ActivateAsync();

        var s = Assert.Single(vm.Services);
        Assert.Equal("redis", s.Name);
        Assert.True(s.IsRunning);
    }

    [Fact]
    public async Task Services_Stop_CallsServiceAndReloads()
    {
        var fake = new FakeHomebrewService
        {
            ServicesList = { new BrewService("redis", "started", "yves", null) },
        };
        var vm = new ServicesViewModel(fake);
        await vm.ActivateAsync();

        await vm.StopCommand.ExecuteAsync(vm.Services[0]);

        Assert.Contains("stop:redis", fake.Maintenance);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Taps_Add_ValidatesFormatAndAdds()
    {
        var fake = new FakeHomebrewService();
        var vm = new TapsViewModel(fake);
        await vm.ActivateAsync();

        // Nom invalide (pas de « / ») → refusé.
        vm.NewTapName = "pasbon";
        await vm.AddCommand.ExecuteAsync(null);
        Assert.Empty(fake.Maintenance);
        Assert.Contains("invalide", vm.StatusMessage);

        // Nom valide → ajouté.
        vm.NewTapName = "felixkratz/formulae";
        await vm.AddCommand.ExecuteAsync(null);
        Assert.Contains("tap:felixkratz/formulae", fake.Maintenance);
        Assert.Contains(vm.Taps, t => t.Name == "felixkratz/formulae");
        Assert.Equal(string.Empty, vm.NewTapName);
    }

    [Fact]
    public async Task Taps_Remove_ConfirmsThenUntaps()
    {
        var fake = new FakeHomebrewService
        {
            TapsList = { new BrewTap("felixkratz/formulae", false, 2, 0, false) },
        };
        var vm = new TapsViewModel(fake);
        await vm.ActivateAsync();

        await vm.RemoveCommand.ExecuteAsync(vm.Taps[0]);
        Assert.NotNull(vm.Confirmation);
        Assert.DoesNotContain("untap:felixkratz/formulae", fake.Maintenance);

        await vm.ConfirmCommand.ExecuteAsync(null);
        Assert.Contains("untap:felixkratz/formulae", fake.Maintenance);
    }

    [Fact]
    public async Task Taps_Remove_OfficialTap_DoesNothing()
    {
        var fake = new FakeHomebrewService();
        var vm = new TapsViewModel(fake);

        await vm.RemoveCommand.ExecuteAsync(new BrewTap("homebrew/core", true, 7000, 0, false));

        Assert.Null(vm.Confirmation);
        Assert.Empty(fake.Maintenance);
    }

    [Fact]
    public void Shell_DefaultsToInstalledScreen()
    {
        var vm = new MainViewModel(new FakeHomebrewService());

        Assert.Equal(7, vm.NavItems.Count);
        Assert.Equal("Installés", vm.SelectedNav?.Title);
        Assert.IsType<InstalledViewModel>(vm.CurrentScreen);
    }
}
