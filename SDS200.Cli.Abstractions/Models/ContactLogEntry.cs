namespace SDS200.Cli.Abstractions.Models;

/// <summary>
/// Represents a single contact (signal lock) event detected by the scanner.
/// A contact is created when RSSI crosses above the detection threshold.
/// Immutable record — use <see cref="FromStatus"/> to create instances.
/// </summary>
public record ContactLogEntry
{
    /// <summary>When the signal lock started (UTC).</summary>
    public required DateTime LockTime { get; init; }

    /// <summary>Frequency in MHz when locked.</summary>
    public required double Frequency { get; init; }

    /// <summary>Modulation mode (e.g., "FM", "DMR").</summary>
    public string Modulation { get; init; } = "---";

    /// <summary>Scanner's current mode (from V_Screen attribute).</summary>
    public string Mode { get; init; } = "---";

    /// <summary>System name at lock time.</summary>
    public string SystemName { get; init; } = "---";

    /// <summary>Channel or department name at lock time.</summary>
    public string ChannelName { get; init; } = "---";

    /// <summary>TGID if applicable (trunk mode only).</summary>
    public string TgId { get; init; } = "---";

    /// <summary>Site name if applicable (trunk mode only).</summary>
    public string SiteName { get; init; } = "---";

    /// <summary>Signal strength when locked (RSSI string, e.g., "S3").</summary>
    public string Rssi { get; init; } = "S0";

    /// <summary>
    /// Duration of this contact in seconds since <see cref="LockTime"/>.
    /// Computed from <see cref="DateTime.UtcNow"/> — changes each time it is read.
    /// </summary>
    public double DurationSeconds => (DateTime.UtcNow - LockTime).TotalSeconds;

    /// <summary>
    /// Creates a <see cref="ContactLogEntry"/> snapshot from the current scanner status.
    /// </summary>
    /// <param name="status">The current scanner status at lock time.</param>
    /// <param name="lockTime">
    /// Optional explicit lock timestamp (UTC).
    /// Defaults to <see cref="DateTime.UtcNow"/> when omitted.
    /// </param>
    public static ContactLogEntry FromStatus(ScannerStatus status, DateTime? lockTime = null)
    {
        return new ContactLogEntry
        {
            LockTime = lockTime ?? DateTime.UtcNow,
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
