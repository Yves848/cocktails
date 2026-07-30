using System;
using System.IO;
using Cocktails.Services;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

public class SettingsStoreTests
{
    private static string TempPath()
        => Path.Combine(Path.GetTempPath(), "cocktails-tests-" + Guid.NewGuid().ToString("N"), "settings.json");

    [Fact]
    public void SaveThenLoad_RoundTripsSetting()
    {
        var path = TempPath();
        try
        {
            var store = new SettingsStore(path);
            store.Save(new AppSettings
            {
                ConfirmBeforeUninstall = false,
                MonitoringEnabled = false,
                NotificationsEnabled = false,
                MonitoringIntervalMinutes = 60,
            });

            Assert.True(File.Exists(path));
            var loaded = store.Load();
            Assert.False(loaded.ConfirmBeforeUninstall);
            Assert.False(loaded.MonitoringEnabled);
            Assert.False(loaded.NotificationsEnabled);
            Assert.Equal(60, loaded.MonitoringIntervalMinutes);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var loaded = new SettingsStore(TempPath()).Load();
        Assert.True(loaded.ConfirmBeforeUninstall);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        var path = TempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ ceci n'est pas du json");
        try
        {
            Assert.True(new SettingsStore(path).Load().ConfirmBeforeUninstall);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
