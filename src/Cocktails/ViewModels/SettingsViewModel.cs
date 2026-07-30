using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>Une option de fréquence de vérification (libellé + intervalle en minutes).</summary>
public sealed record FrequencyOption(string Label, int Minutes);

/// <summary>Écran « Réglages » : confirmation, surveillance, notifications, environnement Homebrew.</summary>
public sealed partial class SettingsViewModel : ScreenViewModel
{
    private bool _analyticsEnabled;
    private bool _analyticsLoaded;

    public SettingsViewModel(IHomebrewService homebrew, AppSettings? settings = null) : base(homebrew)
    {
        Settings = settings ?? new AppSettings();
        StatusMessage = "Réglages.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SettingsViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Réglages";

    public AppSettings Settings { get; }

    /// <summary>Environnement Homebrew réel (version, préfixe, cache), chargé à l'activation.</summary>
    [ObservableProperty]
    public partial BrewEnvironment? Environment { get; set; }

    /// <summary>Télémétrie Homebrew (état lu de brew ; l'écriture appelle brew analytics on/off).</summary>
    public bool AnalyticsEnabled
    {
        get => _analyticsEnabled;
        set
        {
            if (SetProperty(ref _analyticsEnabled, value) && _analyticsLoaded)
            {
                _ = ApplyAnalyticsAsync(value);
            }
        }
    }

    public IReadOnlyList<FrequencyOption> Frequencies { get; } =
    [
        new("Toutes les heures", 60),
        new("Toutes les 6 heures", 360),
        new("Une fois par jour", 1440),
    ];

    public FrequencyOption SelectedFrequency
    {
        get => Frequencies.FirstOrDefault(f => f.Minutes == Settings.MonitoringIntervalMinutes)
               ?? Frequencies[1];
        set
        {
            if (value is not null && value.Minutes != Settings.MonitoringIntervalMinutes)
            {
                Settings.MonitoringIntervalMinutes = value.Minutes;
                OnPropertyChanged();
            }
        }
    }

    protected override Task OnFirstActivatedAsync()
        => RunAsync("Lecture de la configuration Homebrew…", async () =>
        {
            Environment = await Homebrew.GetEnvironmentAsync();
            _analyticsEnabled = await Homebrew.GetAnalyticsEnabledAsync();
            OnPropertyChanged(nameof(AnalyticsEnabled));
            _analyticsLoaded = true;
            StatusMessage = "Réglages.";
        });

    private async Task ApplyAnalyticsAsync(bool enabled)
    {
        try
        {
            await Homebrew.SetAnalyticsAsync(enabled);
            StatusMessage = enabled
                ? "Statistiques Homebrew activées."
                : "Statistiques Homebrew désactivées.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
    }
}
