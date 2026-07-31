using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>Une option de fréquence de vérification (libellé + intervalle en minutes).</summary>
public sealed record FrequencyOption(string Label, int Minutes);

/// <summary>Une option de langue (libellé affiché + valeur).</summary>
public sealed record LanguageOption(string Label, AppLanguage Value);

/// <summary>Écran « Réglages » : confirmation, surveillance, notifications, environnement Homebrew.</summary>
public sealed partial class SettingsViewModel : ScreenViewModel
{
    private bool _analyticsEnabled;
    private bool _analyticsLoaded;

    public SettingsViewModel(IHomebrewService homebrew, AppSettings? settings = null) : base(homebrew)
    {
        Settings = settings ?? new AppSettings();
        StatusMessage = L["Status.Settings"];
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SettingsViewModel() : this(new DesignHomebrewService())
    {
    }

    protected override string TitleKey => "Nav.Settings";

    protected override void OnLanguageChanged()
    {
        Frequencies = BuildFrequencies();
        Languages = BuildLanguages();
        OnPropertyChanged(nameof(Frequencies));
        OnPropertyChanged(nameof(SelectedFrequency));
        OnPropertyChanged(nameof(Languages));
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    /// <summary>Langues proposées (Système + langues concrètes en endonymes).</summary>
    public IReadOnlyList<LanguageOption> Languages { get; private set; } = BuildLanguages();

    private static IReadOnlyList<LanguageOption> BuildLanguages() =>
    [
        new(L["Lang.System"], AppLanguage.System),
        new("English", AppLanguage.English),
        new("Français", AppLanguage.French),
        new("Español", AppLanguage.Spanish),
        new("Deutsch", AppLanguage.German),
    ];

    public LanguageOption SelectedLanguage
    {
        get => Languages.FirstOrDefault(o => o.Value == Settings.Language) ?? Languages[0];
        set
        {
            if (value is not null && value.Value != Settings.Language)
            {
                Settings.Language = value.Value;   // le shell applique la langue (Localizer)
                OnPropertyChanged();
            }
        }
    }

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

    public IReadOnlyList<FrequencyOption> Frequencies { get; private set; } = BuildFrequencies();

    private static IReadOnlyList<FrequencyOption> BuildFrequencies() =>
    [
        new(L["Freq.Hourly"], 60),
        new(L["Freq.6h"], 360),
        new(L["Freq.Daily"], 1440),
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
            StatusMessage = L["Status.Settings"];
        });

    private async Task ApplyAnalyticsAsync(bool enabled)
    {
        try
        {
            await Homebrew.SetAnalyticsAsync(enabled);
            StatusMessage = L[enabled ? "Status.AnalyticsOn" : "Status.AnalyticsOff"];
        }
        catch (Exception ex)
        {
            StatusMessage = L.Format("Error.Generic", ex.Message);
        }
    }
}
