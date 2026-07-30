using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>Écran « Taps » : dépôts tiers — lister, ajouter, retirer, faire confiance.</summary>
public partial class TapsViewModel : ScreenViewModel
{
    public TapsViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public TapsViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Taps";

    public ObservableCollection<BrewTap> Taps { get; } = [];

    /// <summary>Nom du tap à ajouter (forme <c>user/repo</c>).</summary>
    [ObservableProperty]
    public partial string NewTapName { get; set; } = string.Empty;

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync("Chargement des taps…", async () =>
    {
        var taps = await Homebrew.GetTapsAsync();
        Replace(taps);
        StatusMessage = $"{taps.Count} tap(s).";
    });

    [RelayCommand]
    private Task AddAsync()
    {
        var name = NewTapName.Trim();
        // Un tap se nomme user/repo.
        if (name.Length == 0 || !name.Contains('/'))
        {
            StatusMessage = "Nom de tap invalide (attendu : utilisateur/dépôt).";
            return Task.CompletedTask;
        }

        return RunWithOutputAsync($"Ajout du tap « {name} »…", async progress =>
        {
            await Homebrew.AddTapAsync(name, progress);
            NewTapName = string.Empty;
            Replace(await Homebrew.GetTapsAsync());
            StatusMessage = $"Tap « {name} » ajouté.";
        });
    }

    [RelayCommand]
    private Task Remove(BrewTap? tap)
    {
        if (tap is null || tap.Official)
        {
            return Task.CompletedTask;
        }

        return RequestConfirmationTask(
            "Retirer ce tap ?",
            $"« {tap.Name} » sera retiré. Ses formules/casks ne seront plus disponibles à l'installation.",
            "Retirer",
            () => RunWithOutputAsync($"Retrait du tap « {tap.Name} »…", async progress =>
            {
                await Homebrew.RemoveTapAsync(tap.Name, progress);
                Replace(await Homebrew.GetTapsAsync());
                StatusMessage = $"Tap « {tap.Name} » retiré.";
            }));
    }

    [RelayCommand]
    private Task Trust(BrewTap? tap)
    {
        if (tap is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync($"Confiance accordée à « {tap.Name} »…", async progress =>
        {
            await Homebrew.TrustTapAsync(tap.Name, progress);
            StatusMessage = $"« {tap.Name} » approuvé.";
        });
    }

    private void Replace(System.Collections.Generic.IReadOnlyList<BrewTap> taps)
    {
        Taps.Clear();
        foreach (var t in taps)
        {
            Taps.Add(t);
        }
    }

    // RequestConfirmation est void ; on l'enveloppe pour rester dans une chaîne de Task.
    private Task RequestConfirmationTask(string title, string message, string confirmLabel, Func<Task> onConfirm)
    {
        RequestConfirmation(title, message, confirmLabel, onConfirm);
        return Task.CompletedTask;
    }
}
