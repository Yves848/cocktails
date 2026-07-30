using System.Collections.Generic;
using System.Linq;
using Cocktails.Core;

namespace Cocktails.ViewModels;

/// <summary>Une option de fréquence de vérification (libellé + intervalle en minutes).</summary>
public sealed record FrequencyOption(string Label, int Minutes);

/// <summary>Écran « Réglages » : confirmation, surveillance des mises à jour, notifications.</summary>
public sealed class SettingsViewModel : ScreenViewModel
{
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
}
