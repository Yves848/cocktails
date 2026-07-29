using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IHomebrewService _homebrew;

    /// <summary>
    /// Recharge la vue courante (installés / résultats de recherche / obsolètes).
    /// Ré-exécuté après une installation ou une désinstallation pour refléter le
    /// nouvel état sans changer de vue. Ne gère pas l'état occupé : appelé depuis
    /// l'intérieur de <see cref="RunAsync"/>.
    /// </summary>
    private Func<Task> _reload;

    public MainViewModel(IHomebrewService homebrew)
    {
        _homebrew = homebrew;
        _reload = LoadInstalledCoreAsync;
    }

    /// <summary>Constructeur sans argument pour le previewer XAML (design-time).</summary>
    public MainViewModel()
        : this(new DesignHomebrewService())
    {
    }

    public ObservableCollection<Package> Packages { get; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Prêt.";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // --- Chargement des vues ---------------------------------------------------

    [RelayCommand]
    private async Task RefreshInstalledAsync()
    {
        _reload = LoadInstalledCoreAsync;
        await RunAsync("Chargement des packages installés…", _reload);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await RefreshInstalledAsync();
            return;
        }

        _reload = SearchCoreAsync;
        await RunAsync($"Recherche de « {SearchQuery.Trim()} »…", _reload);
    }

    [RelayCommand]
    private async Task ShowOutdatedAsync()
    {
        _reload = LoadOutdatedCoreAsync;
        await RunAsync("Recherche des mises à jour…", _reload);
    }

    // --- Actions par package ---------------------------------------------------

    [RelayCommand]
    private async Task InstallAsync(Package? package)
    {
        if (package is null)
        {
            return;
        }

        await RunAsync($"Installation de « {package.Name} »…", async () =>
        {
            await _homebrew.InstallAsync(package.Name);
            await _reload();
            StatusMessage = $"« {package.Name} » installé.";
        });
    }

    [RelayCommand]
    private async Task UninstallAsync(Package? package)
    {
        if (package is null)
        {
            return;
        }

        await RunAsync($"Désinstallation de « {package.Name} »…", async () =>
        {
            await _homebrew.UninstallAsync(package.Name);
            await _reload();
            StatusMessage = $"« {package.Name} » désinstallé.";
        });
    }

    // --- Loaders (sans garde d'état occupé, réutilisables par _reload) ---------

    private async Task LoadInstalledCoreAsync()
    {
        var installed = await _homebrew.GetInstalledAsync();
        Replace(installed);
        StatusMessage = $"{installed.Count} package(s) installé(s).";
    }

    private async Task SearchCoreAsync()
    {
        var query = SearchQuery.Trim();
        var results = await _homebrew.SearchAsync(query);
        Replace(results);
        StatusMessage = $"{results.Count} résultat(s) pour « {query} ».";
    }

    private async Task LoadOutdatedCoreAsync()
    {
        var outdated = await _homebrew.GetOutdatedAsync();
        Replace(outdated);
        StatusMessage = outdated.Count == 0
            ? "Tout est à jour."
            : $"{outdated.Count} mise(s) à jour disponible(s).";
    }

    private void Replace(IReadOnlyList<Package> packages)
    {
        Packages.Clear();
        foreach (var p in packages)
        {
            Packages.Add(p);
        }
    }

    /// <summary>Exécute une opération en gérant l'état occupé et les erreurs Homebrew.</summary>
    private async Task RunAsync(string busyMessage, Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = busyMessage;
        try
        {
            await action();
        }
        catch (HomebrewException ex)
        {
            StatusMessage = $"Erreur brew : {ex.StandardError}".Trim();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
