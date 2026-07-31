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

    protected override string TitleKey => "Nav.Taps";

    public ObservableCollection<BrewTap> Taps { get; } = [];

    /// <summary>Nom du tap à ajouter (forme <c>user/repo</c>).</summary>
    [ObservableProperty]
    public partial string NewTapName { get; set; } = string.Empty;

    protected override Task OnFirstActivatedAsync() => LoadAsync();

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private Task LoadAsync() => RunAsync(L["Status.LoadingTaps"], async () =>
    {
        var taps = await Homebrew.GetTapsAsync();
        Replace(taps);
        StatusMessage = L.Format("Status.TapsCount", taps.Count);
    });

    [RelayCommand]
    private Task AddAsync()
    {
        var name = NewTapName.Trim();
        // Un tap se nomme user/repo.
        if (name.Length == 0 || !name.Contains('/'))
        {
            StatusMessage = L["Status.TapInvalid"];
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.TapAdding", name), async progress =>
        {
            await Homebrew.AddTapAsync(name, progress);
            NewTapName = string.Empty;
            Replace(await Homebrew.GetTapsAsync());
            StatusMessage = L.Format("Status.TapAdded", name);
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
            L["Confirm.RemoveTapTitle"],
            L.Format("Confirm.RemoveTapMsg", tap.Name),
            L["Confirm.RemoveTapBtn"],
            () => RunWithOutputAsync(L.Format("Status.TapRemoving", tap.Name), async progress =>
            {
                await Homebrew.RemoveTapAsync(tap.Name, progress);
                Replace(await Homebrew.GetTapsAsync());
                StatusMessage = L.Format("Status.TapRemoved", tap.Name);
            }));
    }

    [RelayCommand]
    private Task Trust(BrewTap? tap)
    {
        if (tap is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.TapTrusting", tap.Name), async progress =>
        {
            await Homebrew.TrustTapAsync(tap.Name, progress);
            Replace(await Homebrew.GetTapsAsync());   // rafraîchit l'indicateur de confiance
            StatusMessage = L.Format("Status.TapTrusted", tap.Name);
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
