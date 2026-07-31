using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using Cocktails.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>
/// Une option de fréquence : intervalle en minutes + libellé <b>traduit à la volée</b>
/// (l'instance est stable ; seul <see cref="Display"/> change de langue → pas de
/// reconstruction de l'ItemsSource, qui casserait la sélection du ComboBox).
/// </summary>
public sealed partial class FrequencyOption : ObservableObject
{
    private readonly string _key;

    public FrequencyOption(string labelKey, int minutes)
    {
        _key = labelKey;
        Minutes = minutes;
        Localizer.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Display));
    }

    public int Minutes { get; }

    public string Display => Localizer.Instance[_key];
}

/// <summary>Une option de langue : valeur + libellé affiché (endonyme, ou « Système » traduit).</summary>
public sealed partial class LanguageOption : ObservableObject
{
    private readonly string? _endonym;

    public LanguageOption(AppLanguage value, string? endonym)
    {
        Value = value;
        _endonym = endonym;
        Localizer.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Display));
    }

    public AppLanguage Value { get; }

    public string Display => _endonym ?? Localizer.Instance["Lang.System"];
}

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

    /// <summary>
    /// Langues proposées (Système + endonymes). Liste <b>stable</b> : les instances ne
    /// changent jamais, seuls leurs libellés se traduisent (cf. <see cref="LanguageOption"/>).
    /// </summary>
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new(AppLanguage.System, null),
        new(AppLanguage.English, "English"),
        new(AppLanguage.French, "Français"),
        new(AppLanguage.Spanish, "Español"),
        new(AppLanguage.German, "Deutsch"),
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

    /// <summary>Fréquences proposées (liste stable, libellés traduits à la volée).</summary>
    public IReadOnlyList<FrequencyOption> Frequencies { get; } =
    [
        new("Freq.Hourly", 60),
        new("Freq.6h", 360),
        new("Freq.Daily", 1440),
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
        => RunAsync(L["Status.LoadingConfig"], async () =>
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
