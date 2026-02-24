using System.Text.Json;

namespace SDS200.Cli.Abstractions.Models;

/// <summary>
/// Manages persistent application settings.
/// Saved to settings.json in the application directory.
/// </summary>
public class AppSettings
{
    /// <summary>Last connection mode selected by user ("Serial" or "UDP").</summary>
    public string LastMode { get; set; } = "UDP";

    /// <summary>Last IP address used for UDP connection.</summary>
    public string LastIp { get; set; } = "192.168.1.100";

    /// <summary>Last serial port used for connection.</summary>
    public string LastComPort { get; set; } = "";

    /// <summary>Last baud rate used for serial connection.</summary>
    public int LastBaudRate { get; set; } = 115200;

    /// <summary>
    /// Loads settings from disk, or returns default settings if file doesn't exist.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            var filePath = GetFilePath();
            if (!File.Exists(filePath)) return new AppSettings();
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk as JSON.
    /// </summary>
    public void Save()
    {
        try
        {
            var filePath = GetFilePath();
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not save settings: {ex.Message}");
        }
    }

    private static string GetFilePath() => 
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
}

