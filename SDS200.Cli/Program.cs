using System.Collections.Concurrent;
using Spectre.Console;
using SDS200.Cli.Abstractions;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Logic;
using SDS200.Cli.Presentation;
using System.Diagnostics;

// ── INITIALIZATION ─────────────────────────────────────────────────────────────
var settings = AppSettings.Load();
var status = new ScannerStatus();
var debugLog = new Queue<string>();
var contactLog = new Queue<ContactLogEntry>();
var rawRadioData = new ConcurrentQueue<string>();
var keyboardInputLog = new ConcurrentQueue<string>();
var keyboardCts = new CancellationTokenSource();

bool isDebugger = Debugger.IsAttached;
if (isDebugger) Debugger.Break();

// ── CONNECTION SETUP ───────────────────────────────────────────────────────────
var connectionService = new ConnectionSetupService(settings, isDebugger);
var setupResult = await connectionService.SetupAsync(status, debugLog, rawRadioData);

if (setupResult == null)
{
    return; // Connection setup failed (e.g., no serial ports found)
}

var bridge = setupResult.Bridge;
var contactTracker = new ContactTracker(contactLog);

// ── KEYBOARD HANDLER ───────────────────────────────────────────────────────────
var keyboard = new KeyboardHandler(bridge, keyboardInputLog, debugLog);
var keyboardTask = Task.Run(() => keyboard.RunAsync(keyboardCts.Token));

// ── MAIN POLLING LOOP ──────────────────────────────────────────────────────────
await AnsiConsole.Live(RenderView()).StartAsync(async ctx => {
    while (!keyboard.QuitRequested)
    {
        ctx.UpdateTarget(RenderView());

        // Skip polling when in Command mode - let the user control the communication
        if (keyboard.ViewMode == ViewMode.Command)
        {
            await Task.Delay(100);
            continue;
        }

        // Poll using the GSI command
        string response = await bridge.SendAndReceiveAsync("GSI,0", TimeSpan.FromMilliseconds(500));

        if (response != "TIMEOUT")
        {
            bool parsed = UnidenParser.UpdateStatus(status, response);
            
            if (!parsed) {
                status.LastCommandSent = MarkupConstants.StatusDataUnrecognized;
            } else {
                status.LastCommandSent = MarkupConstants.StatusUpdated;
                contactTracker.ProcessSignalUpdate(status);
            }
        }
        else 
        {
            status.LastCommandSent = MarkupConstants.StatusTimeout;
        }

        await Task.Delay(100);
    }
});

// ── CLEANUP ────────────────────────────────────────────────────────────────────
keyboardCts.Cancel();
try { await keyboardTask; } catch (OperationCanceledException) { }
bridge.Dispose();
AnsiConsole.MarkupLine("[yellow]Goodbye![/]");

// ── RENDER DISPATCHER ──────────────────────────────────────────────────────────
Layout RenderView()
{
    // Command view takes priority (user is manually controlling scanner)
    if (keyboard.ViewMode == ViewMode.Command)
    {
        return CommandViewRenderer.Render(
            bridge.IsConnected,
            keyboard.CommandInput,
            keyboard.CommandHistory);
    }
    
    // Debug view
    if (keyboard.ViewMode == ViewMode.Debug)
    {
        return DebugViewRenderer.Render(
            bridge.IsConnected,
            rawRadioData,
            keyboardInputLog,
            keyboard.SpacebarHeld);
    }
    
    // Menu view when scanner is in menu mode or showing a popup
    if (MenuViewRenderer.IsMenuMode(status))
    {
        return MenuViewRenderer.Render(
            status,
            bridge.IsConnected,
            keyboard.SpacebarHeld);
    }
    
    // Default: Main scanning view
    return MainViewRenderer.Render(
        status,
        bridge.IsConnected,
        contactTracker.GetContacts(),
        keyboard.SpacebarHeld);
}
