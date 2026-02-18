namespace SDS200.Cli.Presentation;

/// <summary>
/// Central repository for all Spectre.Console markup strings used in the UI.
/// All strings are static and pre-validated for markup correctness.
/// </summary>
public static class MarkupConstants
{
    // ── HOTKEY PANELS ──────────────────────────────────────────────────────────
    
    /// <summary>Compact hotkey display (main view)</summary>
    public const string HotkeyMainCompact = "[yellow]D[/] [green]R[/] [cyan]M[/] [magenta]V[/]  (HOLD SPACE for help)";
    
    /// <summary>Expanded hotkey display showing key names (main view)</summary>
    public const string HotkeyMainExpanded = "[yellow][[D]][/] Debug View  [green][[R]][/] Record  [cyan][[M]][/] Mute  [magenta][[V]][/] Volume";
    
    /// <summary>Compact hotkey display (debug view)</summary>
    public const string HotkeyDebugCompact = "[yellow]D[/] [green]R[/] [cyan]M[/] [magenta]V[/]";
    
    /// <summary>Expanded hotkey display showing key names (debug view)</summary>
    public const string HotkeyDebugExpanded = "[yellow][[D]][/] Debug  [green][[R]][/] Record  [cyan][[M]][/] Mute  [magenta][[V]][/] Volume";

    // ── KEYBOARD INPUT LOGGING ─────────────────────────────────────────────────
    
    /// <summary>Keyboard input: D key - Toggle Debug View</summary>
    public const string KeyPressedD = "D (Toggle Debug View)";
    
    /// <summary>Keyboard input: R key - Record Toggle</summary>
    public const string KeyPressedR = "R (Record Toggle)";
    
    /// <summary>Keyboard input: M key - Mute Toggle</summary>
    public const string KeyPressedM = "M (Mute Toggle)";
    
    /// <summary>Keyboard input: V key - Volume Adjust</summary>
    public const string KeyPressedV = "V (Volume)";
    
    /// <summary>Keyboard input: Space key - Hotkey Help Toggle</summary>
    public const string KeyPressedSpace = "SPACE (Hotkey Help)";

    // ── IDENTIFIERS ────────────────────────────────────────────────────────────
    
    public const string LabelSystem = "[blue]SYS:[/]";
    public const string LabelDepartment = "[blue]DEP:[/]";
    public const string LabelSite = "[blue]SITE:[/]";
    public const string LabelTgid = "[yellow]TGID:[/]";
    public const string LabelChannel = "[yellow]CH  :[/]";
    public const string LabelUnitId = "[grey]UID :[/]";
    public const string LabelToneA = "[grey]TnA :[/]";
    public const string LabelToneB = "[grey]TnB :[/]";
    public const string LabelRange = "[grey]RNG :[/]";
    public const string LabelHits = "[grey]HITS:[/]";
    public const string LabelHold = "[red]HOLD:[/]";
    public const string LabelHoldOn = "[bold red]ON[/]";

    // ── SIGNAL PANEL ──────────────────────────────────────────────────────────
    
    public const string FormatRssi = "[bold yellow]{0}[/]";
    public const string FormatVolumeSquelch = "[dim]VOL {0}  SQL {1}[/]";
    public const string FormatMuteAttenuator = "[dim]{0}  ATT:{1}[/]";

    // ── CONNECTION STATUS ──────────────────────────────────────────────────────
    
    public const string StatusConnected = "CONNECTED";
    public const string StatusDisconnected = "DISCONNECTED";
    public const string RecordingIndicator = "[red]REC[/]";
    
    public static string FormatConnectionStatus(bool connected) =>
        $"[{(connected ? "green" : "red")}]{(connected ? StatusConnected : StatusDisconnected)}[/]";
    
    public static string FormatLedIndicator(string ledStatus) =>
        $"  LED:{ledStatus}";

    // ── MODE LABELS ────────────────────────────────────────────────────────────
    
    private static readonly Dictionary<string, string> ModeLabels = new()
    {
        { "conventional_scan", "SCAN" },
        { "trunk_scan", "TRUNK" },
        { "custom_with_scan", "CUSTOM/SCAN" },
        { "cchits_with_scan", "CC HITS" },
        { "custom_search", "CUSTOM SEARCH" },
        { "quick_search", "QUICK SEARCH" },
        { "close_call", "CLOSE CALL" },
        { "cc_searching", "CC SEARCH" },
        { "tone_out", "TONE OUT" },
        { "wx_alert", "WEATHER" },
        { "repeater_find", "RPT FIND" },
        { "reverse_frequency", "REVERSE" },
        { "direct_entry", "DIRECT" },
        { "discovery_conventional", "DISC CONV" },
        { "discovery_trunking", "DISC TRUNK" },
        { "analyze_system_status", "ANALYZE SYS" },
        { "analyze", "WATERFALL" },
    };

    public static string GetModeLabel(string vscreen, string fallback) =>
        ModeLabels.TryGetValue(vscreen, out var label) ? label : fallback;

    // ── PANEL HEADERS ──────────────────────────────────────────────────────────
    
    public const string HeaderIdentity = "Identity";
    public const string HeaderSignal = "Signal";
    public const string HeaderRecentContacts = "Recent Contacts";
    public const string HeaderDebugTraffic = "[bold yellow]DEBUG VIEW - Raw Radio Traffic[/]";

    // ── DEBUG LOG FORMATS ──────────────────────────────────────────────────────
    
    public const string LogGsiReceived = "GSI received";
    public const string LogParseFailed = "Parse failed";
    public const string LogRecordToggle = "Record toggle (TODO)";
    public const string LogMuteToggle = "Mute toggle (TODO)";
    public const string LogVolumeAdjust = "Volume adjust (TODO)";

    public static string FormatDebugLogEntry(string message) =>
        $"[{DateTime.Now:HH:mm:ss}] {message}";

    public static string FormatDebugLogDisplay(string logEntry) =>
        $"[grey]{Spectre.Console.Markup.Escape(logEntry)}[/]";

    // ── MARKUP PATTERNS ────────────────────────────────────────────────────────
    
    public const string BoldWhite = "[bold white]{0}[/]";
    public const string DimText = "[dim]{0}[/]";
    public const string HeaderPanel = "[bold]{0}[/]";

    // ── TGID/CHANNEL DEFAULTS ─────────────────────────────────────────────────
    
    public const string DefaultValue = "---";
    public const string DefaultTgidDisplay = "---";
}
