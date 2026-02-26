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
var fileLogger = new FileLogger();

bool isDebugger = Debugger.IsAttached;
if (isDebugger) Debugger.Break();

// ── CONNECTION SETUP ───────────────────────────────────────────────────────────
var connectionService = new ConnectionSetupService(settings, isDebugMode: isDebugger);
var setupResult = await connectionService.SetupAsync(status, debugLog, rawRadioData);

if (setupResult == null)
{
    return; // Connection setup failed (e.g., no serial ports found)
}

var bridge = setupResult.Bridge;
var contactTracker = new ContactTracker(contactLog);

// ── KEYBOARD HANDLER ───────────────────────────────────────────────────────────
// Keyboard is polled on the main thread inside the Live loop to avoid
// Console handle contention with Spectre.Console's cursor positioning.
var keyboard = new KeyboardHandler(bridge, keyboardInputLog, debugLog);

// ── RENDERER INSTANCES — created once, own their persistent Table widgets ──────
// Spectre.Console Live tracks the rendered shape (_shape) to know how many lines
// to cursor-up before redrawing. Passing a new Layout each frame discards that
// baseline, causing every lower panel to flash on every redraw cycle.
// The fix: own one Layout per view mode, update its panels, then call ctx.Refresh().
var mainRenderer    = new MainViewRenderer();
var debugRenderer   = new DebugViewRenderer();
var menuRenderer    = new MenuViewRenderer();
var commandRenderer = new CommandViewRenderer();

var mainLayout    = mainRenderer.CreateLayout();
var debugLayout   = debugRenderer.CreateLayout();
var menuLayout    = menuRenderer.CreateLayout();
var commandLayout = commandRenderer.CreateLayout();

// Seed initial content so Live has a valid first render
var initialSnap = status.Snapshot();
mainRenderer.Update(mainLayout, initialSnap, bridge.IsConnected, contactTracker.GetContacts(), false);

// ── SYNCHRONIZED OUTPUT ────────────────────────────────────────────────────────
// ANSI Mode 2026 tells the terminal to buffer all output between Begin/End and
// paint it atomically as one screen update. Without this, terminals like Windows
// Terminal paint each line as it arrives — causing visible flicker on large layouts.
const string SyncBegin = "\x1b[?2026h";
const string SyncEnd   = "\x1b[?2026l";

/// <summary>Wraps a Spectre Live refresh in synchronized output markers.</summary>
void SyncRefresh(LiveDisplayContext ctx)
{
    Console.Write(SyncBegin);
    ctx.Refresh();
    Console.Write(SyncEnd);
    Console.Out.Flush();
}

/// <summary>Wraps a Spectre Live target swap in synchronized output markers.</summary>
void SyncUpdateTarget(LiveDisplayContext ctx, Layout target)
{
    Console.Write(SyncBegin);
    ctx.UpdateTarget(target);
    Console.Write(SyncEnd);
    Console.Out.Flush();
}

// ── MAIN POLLING LOOP ──────────────────────────────────────────────────────────
Layout activeLayout = mainLayout;

await AnsiConsole.Live(mainLayout)
    .AutoClear(false)
    .Overflow(VerticalOverflow.Ellipsis)
    .Cropping(VerticalOverflowCropping.Bottom)
    .StartAsync(async ctx =>
    {
        while (!keyboard.QuitRequested)
        {
            // Poll keyboard on the main thread — same thread as Live rendering
            await keyboard.PollKeysAsync();

            // Skip polling when in Command mode — let the user control the communication
            if (keyboard.ViewMode == ViewMode.Command)
            {
                commandRenderer.Update(commandLayout, bridge.IsConnected, keyboard.CommandInput, keyboard.CommandHistory);
                if (!ReferenceEquals(activeLayout, commandLayout)) { SyncUpdateTarget(ctx, commandLayout); activeLayout = commandLayout; }
                else SyncRefresh(ctx);
                await Task.Delay(100);
                continue;
            }

            // Poll using the GSI command.
            // GsiResponseHandler (subscribed to bridge.OnDataReceived) handles XML parsing
            // and status updates via the event pipeline — no duplicate parse needed here.
            string response = await bridge.SendAndReceiveAsync("GSI,0", TimeSpan.FromMilliseconds(500));

            if (response != "TIMEOUT")
            {
                status.LastCommandSent = MarkupConstants.StatusUpdated;

                // Track signal lock state and log new contacts to file
                bool wasLocked = status.SignalLocked;
                contactTracker.ProcessSignalUpdate(status);

                if (!wasLocked && status.SignalLocked)
                {
                    // Signal just locked — log the hit asynchronously (fire-and-forget)
                    _ = fileLogger.LogHitAsync(status.Frequency, status.ChannelName, status.SystemName);
                }
            }
            else
            {
                status.LastCommandSent = MarkupConstants.StatusTimeout;
            }

            // Snapshot status for a consistent read — background thread writes concurrently
            var snap = status.Snapshot();

            // Mutate the correct layout in place then refresh with synchronized output.
            if (keyboard.ViewMode == ViewMode.Debug)
            {
                debugRenderer.Update(debugLayout, bridge.IsConnected, rawRadioData, keyboardInputLog, keyboard.SpacebarHeld);
                if (!ReferenceEquals(activeLayout, debugLayout)) { SyncUpdateTarget(ctx, debugLayout); activeLayout = debugLayout; }
                else SyncRefresh(ctx);
            }
            else if (MenuViewRenderer.IsMenuMode(snap))
            {
                menuRenderer.Update(menuLayout, snap, bridge.IsConnected, keyboard.SpacebarHeld);
                if (!ReferenceEquals(activeLayout, menuLayout)) { SyncUpdateTarget(ctx, menuLayout); activeLayout = menuLayout; }
                else SyncRefresh(ctx);
            }
            else
            {
                mainRenderer.Update(mainLayout, snap, bridge.IsConnected, contactTracker.GetContacts(), keyboard.SpacebarHeld);
                if (!ReferenceEquals(activeLayout, mainLayout)) { SyncUpdateTarget(ctx, mainLayout); activeLayout = mainLayout; }
                else SyncRefresh(ctx);
            }

            await Task.Delay(100);
        }
    });

// ── CLEANUP ────────────────────────────────────────────────────────────────────
bridge.Dispose();
AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
