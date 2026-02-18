using System;
using System.IO;
using System.Text.Json;

namespace SDS200.Cli.Models;

public class AppSettings
{
    public string LastMode { get; set; } = "UDP";
    public string LastIp { get; set; } = "192.168.1.100";
    public string LastComPort { get; set; } = "";
    public int LastBaudRate { get; set; } = 115200;

    private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try {
            if (!File.Exists(FilePath)) return new AppSettings();
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        } catch { return new AppSettings(); }
    }

    public void Save() 
    {
        try {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        } catch (Exception ex) {
            Console.WriteLine($"Could not save settings: {ex.Message}");
        }
    }
}