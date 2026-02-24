// This file exists for backward compatibility.
// AppSettings has been moved to SDS200.Cli.Abstractions.Models.AppSettings
// Import it using: using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Models;

[System.Obsolete("Use SDS200.Cli.Abstractions.Models.AppSettings instead")]
#pragma warning disable CS0649
public class AppSettings
{
    public string LastMode { get; set; } = "UDP";
    public string LastIp { get; set; } = "192.168.1.100";
    public string LastComPort { get; set; } = "";
    public int LastBaudRate { get; set; } = 115200;

    public static AppSettings Load()
    {
        var abstractSettings = SDS200.Cli.Abstractions.Models.AppSettings.Load();
        return new AppSettings
        {
            LastMode = abstractSettings.LastMode,
            LastIp = abstractSettings.LastIp,
            LastComPort = abstractSettings.LastComPort,
            LastBaudRate = abstractSettings.LastBaudRate
        };
    }

    public void Save()
    {
        var abstractSettings = new SDS200.Cli.Abstractions.Models.AppSettings
        {
            LastMode = this.LastMode,
            LastIp = this.LastIp,
            LastComPort = this.LastComPort,
            LastBaudRate = this.LastBaudRate
        };
        abstractSettings.Save();
    }
}
#pragma warning restore CS0649

