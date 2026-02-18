using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;
using Spectre.Console;
using SDS200.Cli;
using SDS200.Cli.Core;
using SDS200.Cli.Bridges;
using SDS200.Cli.Models;
using SDS200.Cli.Logic;
using SDS200.Cli.Presentation;
using System.Diagnostics;

Queue<string> debugLog = new Queue<string>();
Queue<ContactLogEntry> contactLog = new Queue<ContactLogEntry>();
ConcurrentQueue<string> rawRadioData = new ConcurrentQueue<string>(); // Thread-safe for background receive + main render
ConcurrentQueue<string> keyboardInputLog = new ConcurrentQueue<string>(); // Thread-safe for keyboard task + main render
var settings = AppSettings.Load();
var status = new ScannerStatus();
var keyboardCts = new System.Threading.CancellationTokenSource();
IScannerBridge bridge;

// Helper to cap ConcurrentQueue size (no .Count race / no Dequeue on Queue from wrong thread)
void EnqueueCapped(ConcurrentQueue<string> q, string item, int max)
{
    q.Enqueue(item);
    while (q.Count > max) q.TryDequeue(out _);
}

// Buffer to accumulate multi-line GSI responses
StringBuilder gsiBuffer = new StringBuilder();

bool isDebugger = Debugger.IsAttached;
if (isDebugger) Debugger.Break();

AnsiConsole.Write(new FigletText("SDS200 CLI").Color(Color.Orange1));

// If debugging, skip interactive prompts and use last successful connection
string modeChoice;
if (isDebugger)
{
    modeChoice = settings.LastMode == "Serial" ? "Serial (USB)" : "UDP (Network)";
    AnsiConsole.MarkupLine($"[yellow]DEBUG MODE - Using last connection: {modeChoice}[/]");
}
else
{
    modeChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Connect via:")
            .AddChoices("UDP (Network)", "Serial (USB)"));
}

if (modeChoice.StartsWith("UDP")) {
    bridge = new UdpScannerBridge();
    
    if (!isDebugger)
        settings.LastIp = AnsiConsole.Ask<string>($"Scanner IP [grey]({settings.LastIp})[/]:", settings.LastIp);
    
    settings.LastMode = "UDP";

    // Register event handlers BEFORE connecting so the receive loop
    // can deliver data to the debug log from the very first packet.
    bridge.OnDataSent += (data) => {
        // Log sent commands with >> prefix for debug display
        EnqueueCapped(rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($">> {data}"), 30);
    };
    
    bridge.OnDataReceived += (data) => {
        // Store raw data for debug display with << prefix (thread-safe, keeps last 30 responses)
        EnqueueCapped(rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($"<< {data}"), 30);
        
        // Accumulate incoming data until we have a complete GSI response
        gsiBuffer.Append(data);
        string accumulated = gsiBuffer.ToString();
        
        // Only attempt parsing when we have a complete document
        if (accumulated.Contains("</ScannerInfo>"))
        {
            var safeData = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] GSI parsed");
            debugLog.Enqueue(safeData);
            if (debugLog.Count > 5) debugLog.Dequeue();
            
            bool parsed = UnidenParser.UpdateStatus(status, accumulated);
            if (!parsed)
            {
                var errorLog = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] Parse failed");
                debugLog.Enqueue(errorLog);
                if (debugLog.Count > 5) debugLog.Dequeue();
            }
            
            // Reset buffer after processing
            gsiBuffer.Clear();
        }
    };

    AnsiConsole.MarkupLine($"[yellow]Connecting to scanner via UDP ({settings.LastIp})...[/]");
    await bridge.ConnectAsync(settings.LastIp, 50536);

    if (bridge.IsConnected)
        AnsiConsole.MarkupLine("[green]Scanner responding on UDP.[/]");
    else
        AnsiConsole.MarkupLine("[red]No response from scanner — check IP and that the scanner is on the network.[/]");
} else {
    var serialBridge = new SerialScannerBridge();
    bridge = serialBridge;

    string port;
    if (isDebugger)
    {
        // Use last known port in debug mode
        port = settings.LastComPort;
        AnsiConsole.MarkupLine($"[yellow]Connecting to scanner on {port}...[/]");
    }
    else
    {
        // Auto-detect the scanner port (filtered, validated with MDL command)
        AnsiConsole.MarkupLine("[yellow]Searching for SDS200 scanner...[/]");
        var detectedPort = await SerialScannerBridge.DetectScannerPortAsync(
            msg => AnsiConsole.MarkupLine($"[grey]{Markup.Escape(msg)}[/]"));

        if (detectedPort != null)
        {
            AnsiConsole.MarkupLine($"[green]Scanner detected on {Markup.Escape(detectedPort)}[/]");
            port = detectedPort;
        }
        else
        {
            // Fallback to manual selection with filtered ports
            AnsiConsole.MarkupLine("[yellow]Auto-detect failed. Falling back to manual selection.[/]");
            var ports = SerialScannerBridge.GetFilteredPorts();

            if (ports.Length == 0) {
                AnsiConsole.MarkupLine("[red]No serial ports found! Check your hardware connection.[/]");
                return;
            }

            var portPrompt = new SelectionPrompt<string>()
                .Title("Select Serial Port:")
                .AddChoices(ports);

            port = AnsiConsole.Prompt(portPrompt);
        }
    }

    settings.LastComPort = port;
    settings.LastMode = "Serial";

    // Register event handlers before connecting for the serial path too
    bridge.OnDataSent += (data) => {
        // Log sent commands with >> prefix for debug display
        EnqueueCapped(rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($">> {data}"), 30);
    };
    
    bridge.OnDataReceived += (data) => {
        // Store raw data for debug display with << prefix (thread-safe, keeps last 30)
        EnqueueCapped(rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($"<< {data}"), 30);
        
        // Accumulate incoming data until we have a complete GSI response
        gsiBuffer.Append(data);
        string accumulated = gsiBuffer.ToString();
        
        // Only attempt parsing when we have a complete document
        if (accumulated.Contains("</ScannerInfo>"))
        {
            var safeData = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] GSI received");
            debugLog.Enqueue(safeData);
            if (debugLog.Count > 5) debugLog.Dequeue();
            
            bool parsed = UnidenParser.UpdateStatus(status, accumulated);
            if (!parsed)
            {
                var errorLog = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] Parse failed");
                debugLog.Enqueue(errorLog);
                if (debugLog.Count > 5) debugLog.Dequeue();
            }
            
            // Reset buffer after processing
            gsiBuffer.Clear();
        }
    };

    await bridge.ConnectAsync(port, settings.LastBaudRate);

    // Enable event monitoring now that connection is established (avoids race condition)
    serialBridge.EnableEventMonitoring();
}

settings.Save();

// Create keyboard handler with shared state
var keyboard = new KeyboardHandler(bridge, keyboardInputLog, debugLog);

// Start keyboard input handler in background
var keyboardTask = Task.Run(() => keyboard.RunAsync(keyboardCts.Token));

await AnsiConsole.Live(Render(status, bridge.IsConnected, debugLog, contactLog, keyboard, rawRadioData, keyboardInputLog)).StartAsync(async ctx => {
    while (true)
    {
        ctx.UpdateTarget(Render(status, bridge.IsConnected, debugLog, contactLog, keyboard, rawRadioData, keyboardInputLog));

        // Poll using the GSI command (OnDataSent event logs the command automatically)
        string response = await bridge.SendAndReceiveAsync("GSI,0", TimeSpan.FromMilliseconds(500));

        if (response != "TIMEOUT")
        {
            // CRITICAL: We no longer check .StartsWith("GSI") here.
            // We pass the raw response to the parser and let it handle the XML stripping.
            bool parsed = UnidenParser.UpdateStatus(status, response);
            
            if (!parsed) {
                status.LastCommandSent = "⚠️ Data Received but Unrecognized";
            } else {
                status.LastCommandSent = "✅ Status Updated";
                
                // Track contact based on RSSI threshold (> 0 = signal detected)
                bool signalPresent = status.LastRssiValue > 0;
                if (signalPresent && !status.SignalLocked)
                {
                    // Signal just locked on
                    status.SignalLocked = true;
                    status.LastLockChangeTime = DateTime.UtcNow;
                    var entry = ContactLogEntry.FromStatus(status);
                    contactLog.Enqueue(entry);
                    if (contactLog.Count > 30) contactLog.Dequeue();
                }
                else if (!signalPresent && status.SignalLocked)
                {
                    // Signal just dropped
                    status.SignalLocked = false;
                    status.LastLockChangeTime = DateTime.UtcNow;
                }
            }
        }
        else 
        {
            status.LastCommandSent = "⌛ Radio Timeout - Check Connection";
        }

        await Task.Delay(150); // Increased delay slightly for radio processing
    }
    
    // Clean up keyboard task on exit
    keyboardCts.Cancel();
    await keyboardTask;
});

static Layout Render(ScannerStatus s, bool conn, Queue<string> logs, Queue<ContactLogEntry> contacts, KeyboardHandler keyboard, ConcurrentQueue<string> rawRadioData, ConcurrentQueue<string> keyboardInputLog) {
    // If in Debug view, show fullscreen raw data with diagnostics
    if (keyboard.ViewMode == ViewMode.Debug)
    {
        // Status header showing connection and data counts
        string statusLine = DebugDisplayFactory.CreateStatusLine(conn, rawRadioData.Count);
        
        // Build table with raw radio data and keyboard input
        var debugTable = new Table().NoBorder().HideHeaders().AddColumns("Keyboard Input", "Raw Radio Traffic");
        
        // Snapshot both queues (thread-safe ToArray on ConcurrentQueue)
        var keyboardList = keyboardInputLog.ToArray();
        var radioList = rawRadioData.ToArray();
        int maxRows = Math.Max(keyboardList.Length, radioList.Length);
        
        if (maxRows == 0)
        {
            debugTable.AddRow(
                new Markup("[dim]No keyboard input[/]"),
                new Markup("[dim]No radio data received yet. Ensure GSI,0 commands are being sent.[/]")
            );
        }
        else
        {
            for (int i = 0; i < maxRows; i++)
            {
                string keyInput = i < keyboardList.Length 
                    ? DebugDisplayFactory.EscapeForDisplay(keyboardList[i])
                    : "";
                string radioData = i < radioList.Length 
                    ? DebugDisplayFactory.EscapeForDisplay(radioList[i])
                    : "";
                    
                debugTable.AddRow(
                    new Markup(keyInput),
                    new Markup(radioData)
                );
            }
        }
        
        var debugLayout = new Layout("Root")
            .SplitRows(
                new Layout("Status").Size(3),
                new Layout("Data"),
                new Layout("Footer").Size(1)
            );
        
        debugLayout["Status"].Update(new Panel(
            new Markup(statusLine).LeftJustified()
        ).Border(BoxBorder.Rounded));
        
        debugLayout["Data"].Update(new Panel(debugTable).Expand());
        
        string hotkeyTxt = keyboard.SpacebarHeld 
            ? MarkupConstants.HotkeyDebugExpanded
            : MarkupConstants.HotkeyDebugCompact;
        debugLayout["Footer"].Update(new Markup(hotkeyTxt).LeftJustified());
        
        return debugLayout;
    }
    
    // ── MAIN VIEW ──────────────────────────────────────────────────────────────
    
    // Build mode-aware identity table
    var info = new Table().NoBorder().HideHeaders().AddColumns("L", "V")
        .AddRow(MarkupConstants.LabelSystem, Markup.Escape(s.SystemName))
        .AddRow(MarkupConstants.LabelDepartment, Markup.Escape(s.DepartmentName));

    // Show context-appropriate rows based on V_Screen
    switch (s.VScreen)
    {
        case "trunk_scan":
            info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
            // Guard against TGID showing as "TGID" (malformed data)
            string tgidDisplay = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
            info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisplay)));
            info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
            if (s.UnitId != "---") info.AddRow(MarkupConstants.LabelUnitId, Markup.Escape(s.UnitId));
            break;

        case "tone_out":
            info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
            info.AddRow(MarkupConstants.LabelToneA, Markup.Escape(s.ToneA));
            info.AddRow(MarkupConstants.LabelToneB, Markup.Escape(s.ToneB));
            break;

        case "custom_search":
        case "quick_search":
        case "close_call":
        case "cc_searching":
        case "repeater_find":
        case "reverse_frequency":
        case "direct_entry":
            if (s.SearchRangeLower != "---")
                info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
            break;

        case "discovery_conventional":
            if (s.SearchRangeLower != "---")
                info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
            if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
            break;

        case "discovery_trunking":
            info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
            string tgidDisplay2 = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
            info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisplay2)));
            info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
            if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
            break;

        case "analyze_system_status":
            info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
            break;

        case "analyze":
            info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
            info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
            if (s.SearchRangeLower != "---")
                info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
            break;

        default: // conventional_scan, custom_with_scan, cchits_with_scan, wx_alert, etc.
            info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
            break;
    }

    // Hold indicator
    if (s.Hold == "On")
        info.AddRow(MarkupConstants.LabelHold, MarkupConstants.LabelHoldOn);

    // Contact log table (show recent contacts)
    var contactTable = new Table().NoBorder().HideHeaders().AddColumns("Freq", "Mode", "ID", "Dur");
    int contactCount = 0;
    foreach(var contact in contacts)
    {
        if (contactCount >= 5) break; // Show only last 5
        string contactId = contact.TgId != "---" ? contact.TgId : contact.ChannelName;
        int durSec = (int)contact.DurationSeconds;
        contactTable.AddRow(
            $"{contact.Frequency:F4}",
            contact.Mode,
            Markup.Escape(contactId),
            $"{durSec}s"
        );
        contactCount++;
    }

    // Mode label for the hero panel header
    string modeLabel = MarkupConstants.GetModeLabel(s.VScreen, s.Mode);

    var layout = new Layout("Root")
        .SplitRows(
            new Layout("Hero"),
            new Layout("Mid").SplitColumns(new Layout("Data"), new Layout("RSSI")),
            new Layout("Contacts").Size(6),
            new Layout("Hotkeys").Size(1),
            new Layout("Footer").Size(3)
        );

    var freqFiglet = new FigletText($"{s.Frequency:F4} MHz")
        .Color(Color.Green)
        .Centered();
    var heroContent = new Rows(
        freqFiglet,
        new Markup($"[dim]{Markup.Escape(s.Modulation)}[/]").Centered()
    );
    layout["Hero"].Update(new Panel(heroContent)
        .Header(string.Format(MarkupConstants.HeaderPanel, Markup.Escape(modeLabel)))
        .Border(BoxBorder.Double));

    layout["Data"].Update(new Panel(info).Header(MarkupConstants.HeaderIdentity).Expand());
    
    // Signal panel — show RSSI plus key property indicators
    var signalRows = new Rows(
        new Markup(string.Format(MarkupConstants.FormatRssi, Markup.Escape(s.Rssi))),
        new Markup(string.Format(MarkupConstants.FormatVolumeSquelch, s.Volume, s.Squelch)),
        new Markup(string.Format(MarkupConstants.FormatMuteAttenuator, Markup.Escape(s.Mute), Markup.Escape(s.Attenuator)))
    );
    layout["RSSI"].Update(new Panel(
        Align.Center(signalRows, VerticalAlignment.Middle))
        .Header(MarkupConstants.HeaderSignal));

    // Contact log panel
    layout["Contacts"].Update(new Panel(contactTable).Header(MarkupConstants.HeaderRecentContacts).Expand());
    
    // Hotkey panel - show expanded or compact based on spacebar
    string hotkeyText = keyboard.SpacebarHeld
        ? MarkupConstants.HotkeyMainExpanded
        : MarkupConstants.HotkeyMainCompact;
    layout["Hotkeys"].Update(new Markup(hotkeyText).LeftJustified());

    string connText = MarkupConstants.FormatConnectionStatus(conn);
    string statusExtra = s.Recording == "On" ? $"  {MarkupConstants.RecordingIndicator}" : "";
    string ledExtra = s.AlertLed != "Off" ? MarkupConstants.FormatLedIndicator(Markup.Escape(s.AlertLed)) : "";
    layout["Footer"].Update(new Panel(
        new Markup($"{connText}{statusExtra}{ledExtra}"))
        .Border(BoxBorder.None));

    return layout;
}