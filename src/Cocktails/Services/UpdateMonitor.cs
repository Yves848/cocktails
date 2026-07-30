using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Cocktails.Core;
using Cocktails.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.Services;

/// <summary>
/// Surveille périodiquement les paquets obsolètes (<c>brew outdated</c>), expose leur
/// nombre (<see cref="OutdatedCount"/>, pour le badge de navigation) et notifie quand de
/// <b>nouveaux</b> paquets deviennent obsolètes. La logique de vérification
/// (<see cref="CheckNowAsync"/>) est indépendante du timer, donc testable.
/// </summary>
public sealed partial class UpdateMonitor : ObservableObject
{
    private readonly IHomebrewService _homebrew;
    private readonly AppSettings _settings;
    private readonly INotifier _notifier;

    private HashSet<string> _known = new(StringComparer.Ordinal);
    private bool _firstCheckDone;
    private DispatcherTimer? _timer;

    [ObservableProperty]
    public partial int OutdatedCount { get; private set; }

    public UpdateMonitor(IHomebrewService homebrew, AppSettings settings, INotifier notifier)
    {
        _homebrew = homebrew;
        _settings = settings;
        _notifier = notifier;
    }

    /// <summary>
    /// Effectue une vérification : met à jour le compteur et notifie les nouveautés.
    /// Les erreurs (brew indisponible…) sont ignorées silencieusement.
    /// </summary>
    public async Task CheckNowAsync()
    {
        IReadOnlyList<Cocktails.Core.Models.Package> outdated;
        try
        {
            outdated = await _homebrew.GetOutdatedAsync().ConfigureAwait(true);
        }
        catch (Exception)
        {
            return;
        }

        OutdatedCount = outdated.Count;
        var names = outdated.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        // On ne notifie pas au tout premier passage (état initial), ni si désactivé.
        if (_firstCheckDone && _settings.NotificationsEnabled)
        {
            var fresh = names.Where(n => !_known.Contains(n)).ToList();
            if (fresh.Count > 0)
            {
                await _notifier.NotifyAsync("Cocktails", FormatMessage(fresh)).ConfigureAwait(true);
            }
        }

        _known = names;
        _firstCheckDone = true;
    }

    /// <summary>Démarre la surveillance (timer piloté par les réglages).</summary>
    public void Start()
    {
        _settings.PropertyChanged += OnSettingsChanged;
        Reconfigure();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppSettings.MonitoringEnabled)
            or nameof(AppSettings.MonitoringIntervalMinutes))
        {
            Reconfigure();
        }
    }

    private void Reconfigure()
    {
        _timer?.Stop();
        _timer = null;

        if (!_settings.MonitoringEnabled)
        {
            OutdatedCount = 0;
            _firstCheckDone = false;
            _known = new HashSet<string>(StringComparer.Ordinal);
            return;
        }

        var minutes = Math.Max(1, _settings.MonitoringIntervalMinutes);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
        _timer.Tick += (_, _) => _ = CheckNowAsync();
        _timer.Start();
        _ = CheckNowAsync();   // vérification immédiate au démarrage
    }

    private static string FormatMessage(IReadOnlyList<string> fresh)
    {
        var names = string.Join(", ", fresh.Take(3));
        var suffix = fresh.Count > 3 ? "…" : string.Empty;
        return fresh.Count == 1
            ? $"Mise à jour disponible : {names}"
            : $"{fresh.Count} nouvelles mises à jour : {names}{suffix}";
    }
}
