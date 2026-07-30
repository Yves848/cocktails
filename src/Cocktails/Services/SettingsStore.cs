using System;
using System.IO;
using System.Text.Json;
using Cocktails.ViewModels;

namespace Cocktails.Services;

/// <summary>
/// Persistance des réglages dans un fichier JSON
/// (<c>~/.config/Cocktails/settings.json</c> par défaut). Tolérant aux erreurs :
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
                    return new AppSettings { ConfirmBeforeUninstall = dto.ConfirmBeforeUninstall };
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
            var dto = new SettingsDto { ConfirmBeforeUninstall = settings.ConfirmBeforeUninstall };
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
    }
}
