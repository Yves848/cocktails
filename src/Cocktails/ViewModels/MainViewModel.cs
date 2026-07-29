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

    public MainViewModel(IHomebrewService homebrew)
    {
        _homebrew = homebrew;
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

    [RelayCommand]
    private async Task RefreshInstalledAsync()
    {
        await RunAsync("Chargement des packages installés…", async () =>
        {
            var installed = await _homebrew.GetInstalledAsync();
            Replace(installed);
            StatusMessage = $"{installed.Count} package(s) installé(s).";
        });
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await RefreshInstalledAsync();
            return;
        }

        await RunAsync($"Recherche de « {SearchQuery} »…", async () =>
        {
            var results = await _homebrew.SearchAsync(SearchQuery.Trim());
            Replace(results);
            StatusMessage = $"{results.Count} résultat(s) pour « {SearchQuery.Trim()} ».";
        });
    }

    [RelayCommand]
    private async Task ShowOutdatedAsync()
    {
        await RunAsync("Recherche des mises à jour…", async () =>
        {
            var outdated = await _homebrew.GetOutdatedAsync();
            Replace(outdated);
            StatusMessage = outdated.Count == 0
                ? "Tout est à jour."
                : $"{outdated.Count} mise(s) à jour disponible(s).";
        });
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
