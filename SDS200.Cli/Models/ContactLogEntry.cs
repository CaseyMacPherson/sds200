// This file exists for backward compatibility.
// ContactLogEntry has been moved to SDS200.Cli.Abstractions.Models.ContactLogEntry
// Import it using: using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Models;

[System.Obsolete("Use SDS200.Cli.Abstractions.Models.ContactLogEntry instead")]
#pragma warning disable CS0649
public class ContactLogEntry
{
    public DateTime LockTime { get; set; }
    public double Frequency { get; set; }
    public string Modulation { get; set; } = "---";
    public string Mode { get; set; } = "---";
    public string SystemName { get; set; } = "---";
    public string ChannelName { get; set; } = "---";
    public string TgId { get; set; } = "---";
    public string SiteName { get; set; } = "---";
    public string Rssi { get; set; } = "S0";
    public double DurationSeconds => (DateTime.UtcNow - LockTime).TotalSeconds;

    public static ContactLogEntry FromStatus(ScannerStatus status) =>
        new ContactLogEntry
        {
            LockTime = DateTime.UtcNow,
            Frequency = status.Frequency,
            Modulation = status.Modulation,
            Mode = status.VScreen,
            SystemName = status.SystemName,
            ChannelName = status.ChannelName,
            TgId = status.TgId,
            SiteName = status.SiteName,
            Rssi = status.Rssi
        };
}
#pragma warning restore CS0649

