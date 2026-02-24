namespace SDS200.Cli.Abstractions.Models;

/// <summary>
/// Represents a single contact (signal lock) event detected by the scanner.
/// A contact is created when RSSI crosses above a threshold.
/// </summary>
public class ContactLogEntry
{
    /// <summary>When the signal lock started.</summary>
    public DateTime LockTime { get; set; }

    /// <summary>Frequency in MHz when locked.</summary>
    public double Frequency { get; set; }

    /// <summary>Modulation mode (e.g., "FM", "DMR").</summary>
    public string Modulation { get; set; } = "---";

    /// <summary>Scanner's current mode (from V_Screen attribute).</summary>
    public string Mode { get; set; } = "---";

    /// <summary>System name at lock time.</summary>
    public string SystemName { get; set; } = "---";

    /// <summary>Channel or department name at lock time.</summary>
    public string ChannelName { get; set; } = "---";

    /// <summary>TGID if applicable (trunk mode only).</summary>
    public string TgId { get; set; } = "---";

    /// <summary>Site name if applicable (trunk mode only).</summary>
    public string SiteName { get; set; } = "---";

    /// <summary>Signal strength when locked (RSSI value).</summary>
    public string Rssi { get; set; } = "S0";

    /// <summary>Get the duration of this contact in seconds since lock.</summary>
    public double DurationSeconds => (DateTime.UtcNow - LockTime).TotalSeconds;

    /// <summary>
    /// Create a contact entry from current scanner status.
    /// </summary>
    public static ContactLogEntry FromStatus(ScannerStatus status)
    {
        return new ContactLogEntry
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
}

