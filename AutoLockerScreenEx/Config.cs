using System.IO;
using System.Text.Json;

namespace AutoLockerScreenEx;

/// <summary>
/// App config, stored as JSON in ~/.config/auto_locker_screen_ex/config.json.
/// Loaded at startup, saved whenever a value changes.
/// </summary>
public class Config
{
    public bool MonitorEnabled { get; set; } = true;
    public uint MonitorIntervalSeconds { get; set; } = 60;
    public bool AutoResetEnabled { get; set; }
    public uint AutoResetSeconds { get; set; } = 300;

    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "auto_locker_screen_ex", "config.json");

    public static Config Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath)) ?? new Config();
            }
        }
        catch
        {
            // corrupt/unreadable config -> fall back to defaults
        }
        var fresh = new Config();
        fresh.Save(); // make the file discoverable on first run
        return fresh;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
