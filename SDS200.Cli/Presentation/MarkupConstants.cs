namespace SDS200.Cli.Presentation;

/// <summary>
/// Central repository for all Spectre.Console markup strings used in the UI.
/// All strings are static and pre-validated for markup correctness.
/// </summary>
public static class MarkupConstants
{
    // ── HOTKEY PANELS ──────────────────────────────────────────────────────────
    
    /// <summary>Compact hotkey display (main view)</summary>
    public const string HotkeyMainCompact = "[yellow]D[/] [green]R[/] [cyan]M[/] [magenta]V[/] [blue]C[/] [red]Q[/]  (HOLD SPACE for help)";
    
    /// <summary>Expanded hotkey display showing key names (main view)</summary>
    public const string HotkeyMainExpanded = "[yellow][[D]][/] Debug  [green][[R]][/] Record  [cyan][[M]][/] Mute  [magenta][[V]][/] Volume  [blue][[C]][/] Command  [red][[Q]][/] Quit";
    
    /// <summary>Compact hotkey display (debug view)</summary>
    public const string HotkeyDebugCompact = "[yellow]D[/] [green]R[/] [cyan]M[/] [magenta]V[/] [blue]C[/] [red]Q[/] [dim]ESC[/]";
    
    /// <summary>Expanded hotkey display showing key names (debug view)</summary>
    public const string HotkeyDebugExpanded = "[yellow][[D]][/] Main  [green][[R]][/] Record  [cyan][[M]][/] Mute  [magenta][[V]][/] Volume  [blue][[C]][/] Command  [red][[Q]][/] Quit  [dim][[ESC]][/] Exit";

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
    
    /// <summary>Keyboard input: C key - Command Mode</summary>
    public const string KeyPressedC = "C (Command Mode)";
    
    /// <summary>Keyboard input: Q key - Quit</summary>
    public const string KeyPressedQ = "Q (Quit)";

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

    /// <summary>
    /// Formats a debug log entry with a pre-formatted timestamp.
    /// Callers should obtain the timestamp from <c>ITimeProvider</c>.
    /// </summary>
    public static string FormatDebugLogEntry(DateTime timestamp, string message) =>
        $"[{timestamp:HH:mm:ss}] {message}";

    public static string FormatDebugLogDisplay(string logEntry) =>
        $"[grey]{Spectre.Console.Markup.Escape(logEntry)}[/]";

    // ── MARKUP PATTERNS ────────────────────────────────────────────────────────
    
    public const string BoldWhite = "[bold white]{0}[/]";
    public const string DimText = "[dim]{0}[/]";
    public const string HeaderPanel = "[bold]{0}[/]";

    // ── TGID/CHANNEL DEFAULTS ─────────────────────────────────────────────────
    
    public const string DefaultValue = "---";
    public const string DefaultTgidDisplay = "---";

    // ── STARTUP MESSAGES ───────────────────────────────────────────────────────
    
    /// <summary>Debug mode startup message showing last connection type</summary>
    public static string FormatDebugModeStartup(string connectionType) =>
        $"[yellow]DEBUG MODE - Using last connection: {Spectre.Console.Markup.Escape(connectionType)}[/]";
    
    /// <summary>Scanner IP input prompt with default value shown</summary>
    public static string FormatScannerIpPrompt(string defaultIp) =>
        $"Scanner IP [grey]({Spectre.Console.Markup.Escape(defaultIp)})[/]:";
    
    /// <summary>UDP connection attempt message</summary>
    public static string FormatUdpConnecting(string ip) =>
        $"[yellow]Connecting to scanner via UDP ({Spectre.Console.Markup.Escape(ip)})...[/]";
    
    /// <summary>Serial connection attempt message</summary>
    public static string FormatSerialConnecting(string port) =>
        $"[yellow]Connecting to scanner on {Spectre.Console.Markup.Escape(port)}...[/]";
    
    /// <summary>Scanner responding on UDP success message</summary>
    public const string UdpConnectedSuccess = "[green]Scanner responding on UDP.[/]";
    
    /// <summary>Scanner not responding on UDP error message</summary>
    public const string UdpConnectedFailure = "[red]No response from scanner — check IP and that the scanner is on the network.[/]";
    
    /// <summary>Searching for scanner message</summary>
    public const string SearchingForScanner = "[yellow]Searching for SDS200 scanner...[/]";
    
    /// <summary>Scanner detected success message</summary>
    public static string FormatScannerDetected(string port) =>
        $"[green]Scanner detected on {Spectre.Console.Markup.Escape(port)}[/]";
    
    /// <summary>Auto-detect failed message</summary>
    public const string AutoDetectFailed = "[yellow]Auto-detect failed. Falling back to manual selection.[/]";
    
    /// <summary>No serial ports found error message</summary>
    public const string NoSerialPortsFound = "[red]No serial ports found! Check your hardware connection.[/]";
    
    /// <summary>Format a grey status message (for auto-detect progress)</summary>
    public static string FormatGreyMessage(string message) =>
        $"[grey]{Spectre.Console.Markup.Escape(message)}[/]";

    // ── DEBUG VIEW PLACEHOLDERS ────────────────────────────────────────────────
    
    /// <summary>Placeholder when no keyboard input has been recorded</summary>
    public const string NoKeyboardInput = "[dim]No keyboard input[/]";
    
    /// <summary>Placeholder when no radio data has been received</summary>
    public const string NoRadioData = "[dim]No radio data received yet. Ensure GSI,0 commands are being sent.[/]";
    
    /// <summary>Format modulation display (dim text)</summary>
    public static string FormatModulation(string modulation) =>
        $"[dim]{Spectre.Console.Markup.Escape(modulation)}[/]";

    // ── STATUS INDICATORS (for ScannerStatus.LastCommandSent) ──────────────────
    
    /// <summary>Status: Data received but could not be parsed</summary>
    public const string StatusDataUnrecognized = "[yellow]WARN[/] Data Received but Unrecognized";
    
    /// <summary>Status: Scanner status successfully updated</summary>
    public const string StatusUpdated = "[green]OK[/] Status Updated";
    
    /// <summary>Status: Radio timeout occurred</summary>
    public const string StatusTimeout = "[red]TIMEOUT[/] Radio Timeout - Check Connection";

    // ── MENU VIEW ──────────────────────────────────────────────────────────────
    
    /// <summary>Menu view header</summary>
    public const string HeaderMenu = "[bold]Scanner Menu[/]";
    
    /// <summary>Prompt header when scanner shows a dialog</summary>
    public const string HeaderPrompt = "[bold red]Scanner Prompt[/]";
    
    /// <summary>Menu mode footer indicator</summary>
    public const string MenuModeIndicator = "[dim]Menu Mode[/]";
    
    /// <summary>No menu information available placeholder</summary>
    public const string NoMenuInfo = "[dim]No menu information available[/]";
    
    /// <summary>No active prompt placeholder</summary>
    public const string NoActivePrompt = "[dim]No active prompt[/]";

    // ── COMMAND VIEW ───────────────────────────────────────────────────────────
    
    /// <summary>Command view header</summary>
    public const string HeaderCommand = "[bold blue]COMMAND MODE[/]";
    
    /// <summary>Command mode instructions</summary>
    public const string CommandModeInstructions = "[dim]Type command and press ENTER to send. ESC to return to scan view.[/]";
    
    /// <summary>Command input prompt</summary>
    public const string CommandInputPrompt = "[cyan]>[/] ";
    
    /// <summary>No command history placeholder</summary>
    public const string NoCommandHistory = "[dim]No commands sent yet. Try: MDL, VER, STS, GSI,0[/]";
    
    /// <summary>Command mode footer indicator</summary>
    public const string CommandModeIndicator = "[dim]Command Mode - Polling Paused[/]";
    
    /// <summary>Hotkey display for command mode</summary>
    public const string HotkeyCommandMode = "[dim]ESC[/] Exit Command Mode";
}
