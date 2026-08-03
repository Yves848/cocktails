using System;
using System.IO;
using System.Text.Json;
using Cocktails.ViewModels;

namespace Cocktails.Services;

/// <summary>
/// Persistance des réglages dans un fichier JSON, sous le dossier de données de l'app
/// (<see cref="Environment.SpecialFolder.ApplicationData"/> → sur macOS
/// <c>~/Library/Application Support/Cocktails/settings.json</c>). Tolérant aux erreurs :
/// un fichier manquant ou corrompu retombe sur les valeurs par défaut, et les échecs
/// d'écriture sont ignorés (les réglages restent au moins valides en mémoire).
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public SettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cocktails",
            "settings.json");
    }

    /// <summary>Charge les réglages, ou les valeurs par défaut si absent/illisible.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(_path));
                if (dto is not null)
                {
                    return new AppSettings
                    {
                        ConfirmBeforeUninstall = dto.ConfirmBeforeUninstall,
                        MonitoringEnabled = dto.MonitoringEnabled,
                        NotificationsEnabled = dto.NotificationsEnabled,
                        MonitoringIntervalMinutes = dto.MonitoringIntervalMinutes,
                        KeepRunningInBackground = dto.KeepRunningInBackground,
                        Language = dto.Language,
                        TerminalShortcut = dto.TerminalShortcut,
                        WindowWidth = dto.WindowWidth,
                        WindowHeight = dto.WindowHeight,
                        WindowX = dto.WindowX,
                        WindowY = dto.WindowY,
                        WindowMaximized = dto.WindowMaximized,
                    };
                }
            }
        }
        catch (Exception)
        {
            // Fichier corrompu / illisible → valeurs par défaut.
        }

        return new AppSettings();
    }

    /// <summary>Écrit les réglages sur disque (échec silencieux).</summary>
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var dto = new SettingsDto
            {
                ConfirmBeforeUninstall = settings.ConfirmBeforeUninstall,
                MonitoringEnabled = settings.MonitoringEnabled,
                NotificationsEnabled = settings.NotificationsEnabled,
                MonitoringIntervalMinutes = settings.MonitoringIntervalMinutes,
                KeepRunningInBackground = settings.KeepRunningInBackground,
                Language = settings.Language,
                TerminalShortcut = settings.TerminalShortcut,
                WindowWidth = settings.WindowWidth,
                WindowHeight = settings.WindowHeight,
                WindowX = settings.WindowX,
                WindowY = settings.WindowY,
                WindowMaximized = settings.WindowMaximized,
            };
            File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (Exception)
        {
            // Écriture impossible → on garde au moins les réglages en mémoire.
        }
    }

    /// <summary>Forme sérialisée (découplée du view model observable).</summary>
    private sealed class SettingsDto
    {
        public bool ConfirmBeforeUninstall { get; set; } = true;
        public bool MonitoringEnabled { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public int MonitoringIntervalMinutes { get; set; } = 360;
        public bool KeepRunningInBackground { get; set; } = true;
        public Cocktails.Localization.AppLanguage Language { get; set; } = Cocktails.Localization.AppLanguage.System;
        public string TerminalShortcut { get; set; } = "Cmd+T";
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
        public int? WindowX { get; set; }
        public int? WindowY { get; set; }
        public bool WindowMaximized { get; set; }
    }
}
