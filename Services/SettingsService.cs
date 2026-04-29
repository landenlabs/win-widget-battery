// Copyright (c) 2026
using System.IO;
using System.Text.Json;
using WinWidgetBattery.Models;

namespace WinWidgetBattery.Services;

public static class SettingsService
{
    private static readonly string AppDataPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WinWidgetBattery"
    );

    private static readonly string SettingsFile = System.IO.Path.Combine(AppDataPath, "settings.json");

    static SettingsService()
    {
        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Silently fail and return default settings
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Silently fail
        }
    }
}
