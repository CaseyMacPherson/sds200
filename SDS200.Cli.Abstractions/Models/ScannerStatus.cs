namespace SDS200.Cli.Abstractions.Models;

/// <summary>
/// Represents the current state of the SDS200 scanner.
/// Updated on each GSI,0 response from the scanner.
/// </summary>
public class ScannerStatus
{
    // Scanner mode info
    public string Mode { get; set; } = "---";
    public string VScreen { get; set; } = "---";

    // Identity / hierarchy (common across scan modes)
    public string MonitorListName { get; set; } = "---";
    public string SystemName { get; set; } = "SCANNING";
    public string DepartmentName { get; set; } = "...";
    public string SiteName { get; set; } = "---";
    public string ChannelName { get; set; } = "...";

    // Frequency & modulation
    public double Frequency { get; set; }
    public string Modulation { get; set; } = "---";

    // Trunking
    public string TgId { get; set; } = "---";
    public string UnitId { get; set; } = "---";
    public string ServiceType { get; set; } = "---";

    // Tone-out
    public string ToneA { get; set; } = "---";
    public string ToneB { get; set; } = "---";

    // Search / Discovery
    public string SearchRangeLower { get; set; } = "---";
    public string SearchRangeUpper { get; set; } = "---";
    public int HitCount { get; set; }

    // Property block (always present)
    public string Rssi { get; set; } = "S0";
    public int LastRssiValue { get; set; } = 0; // Numeric RSSI for threshold detection
    public bool SignalLocked { get; set; } = false; // Whether signal is above threshold
    public DateTime LastLockChangeTime { get; set; } = DateTime.UtcNow; // When lock status changed
    public int Volume { get; set; }
    public int Squelch { get; set; }
    public string Mute { get; set; } = "Unmute";
    public string Attenuator { get; set; } = "Off";
    public string AlertLed { get; set; } = "Off";
    public string P25Status { get; set; } = "---";
    public string Hold { get; set; } = "Off";
    public string Recording { get; set; } = "Off";

    // ViewDescription - Menu and popup display information
    public List<string> InfoLines { get; set; } = new();  // InfoArea1-20 text lines
    public string PopupText { get; set; } = "";           // PopupScreen text (e.g., "Quick Save?\n")
    public bool IsInMenu { get; set; } = false;           // Whether scanner is in menu mode
    public string MenuTitle { get; set; } = "";           // Current menu title if available

    // UI state
    public string LastCommandSent { get; set; } = "None";
}

