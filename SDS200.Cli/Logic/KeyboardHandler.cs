using System.Collections.Concurrent;
using SDS200.Cli.Abstractions;
using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Presentation;

namespace SDS200.Cli.Logic;

/// <summary>
/// Handles non-blocking keyboard input polling and dispatches key actions
/// (view toggle, mute, record, volume, spacebar help, command mode, quit).
/// </summary>
public class KeyboardHandler
{
    private readonly IScannerBridge _bridge;
    private readonly ConcurrentQueue<string> _keyboardLog;
    private readonly Queue<string> _debugLog;
    private readonly int _maxKeyboardLogSize;
    private readonly ITimeProvider _timeProvider;

    // Observable state — read by the render loop
    public ViewMode ViewMode { get; set; } = ViewMode.Main;
    public bool SpacebarHeld { get; set; }
    public bool MuteState { get; private set; }
    public bool RecordState { get; private set; }
    
    // Quit state
    public bool QuitRequested { get; private set; }
    
    // Command mode state
    public string CommandInput { get; private set; } = "";
    public ConcurrentQueue<string> CommandHistory { get; } = new();
    private const int MaxCommandHistorySize = 50;

    /// <summary>
    /// Creates a new KeyboardHandler.
    /// </summary>
    /// <param name="bridge">Scanner bridge for sending commands (MUT, REC).</param>
    /// <param name="keyboardLog">Thread-safe queue that receives timestamped key-press log entries.</param>
    /// <param name="debugLog">High-level event log (not thread-safe — only called from the keyboard task).</param>
    /// <param name="timeProvider">Time provider for timestamps (optional, defaults to system time).</param>
    /// <param name="maxKeyboardLogSize">Maximum entries kept in <paramref name="keyboardLog"/>.</param>
    public KeyboardHandler(
        IScannerBridge bridge,
        ConcurrentQueue<string> keyboardLog,
        Queue<string> debugLog,
        ITimeProvider? timeProvider = null,
        int maxKeyboardLogSize = 10)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _keyboardLog = keyboardLog ?? throw new ArgumentNullException(nameof(keyboardLog));
        _debugLog = debugLog ?? throw new ArgumentNullException(nameof(debugLog));
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _maxKeyboardLogSize = maxKeyboardLogSize;
    }

    /// <summary>
    /// Runs the keyboard polling loop until the token is cancelled.
    /// Call via Task.Run(() => handler.RunAsync(cts.Token)).
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await PollKeysAsync();
            await Task.Delay(50, cancellationToken);
        }
    }

    /// <summary>
    /// Non-blocking: drains all available keys from the console input buffer.
    /// Call this on the **same thread** as Spectre.Console Live to avoid
    /// console handle contention that causes display flashing.
    /// </summary>
    public async Task PollKeysAsync()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            await HandleKeyAsync(key);
        }
    }

    /// <summary>
    /// Processes a single key press. Extracted for testability.
    /// </summary>
    public async Task HandleKeyAsync(ConsoleKeyInfo key)
    {
        // In Command mode, handle input differently
        if (ViewMode == ViewMode.Command)
        {
            await HandleCommandModeKeyAsync(key);
            return;
        }

        // ESC key - return to Main view from Debug view
        if (key.Key == ConsoleKey.Escape)
        {
            if (ViewMode == ViewMode.Debug)
            {
                ViewMode = ViewMode.Main;
                EnqueueDebug("Exited Debug View");
            }
            return;
        }

        string keyName = key.KeyChar switch
        {
            'D' or 'd' => MarkupConstants.KeyPressedD,
            'R' or 'r' => MarkupConstants.KeyPressedR,
            'M' or 'm' => MarkupConstants.KeyPressedM,
            'V' or 'v' => MarkupConstants.KeyPressedV,
            'C' or 'c' => MarkupConstants.KeyPressedC,
            'Q' or 'q' => MarkupConstants.KeyPressedQ,
            ' ' => MarkupConstants.KeyPressedSpace,
            _ => $"Unknown: {key.KeyChar}"
        };

        // Log the key press
        var logEntry = DebugDisplayFactory.CreateKeyboardLogEntry(keyName);
        EnqueueCapped(_keyboardLog, logEntry, _maxKeyboardLogSize);

        switch (key.KeyChar)
        {
            case 'D' or 'd':
                ViewMode = ViewMode == ViewMode.Main ? ViewMode.Debug : ViewMode.Main;
                break;

            case 'C' or 'c':
                ViewMode = ViewMode.Command;
                CommandInput = "";
                EnqueueDebug("Entered Command Mode");
                break;

            case 'Q' or 'q':
                QuitRequested = true;
                EnqueueDebug("Quit requested");
                break;

            case 'R' or 'r':
                RecordState = !RecordState;
                await _bridge.SendCommandAsync(RecordState ? "REC,ON" : "REC,OFF");
                EnqueueDebug($"Record {(RecordState ? "ON" : "OFF")}");
                break;

            case 'M' or 'm':
                MuteState = !MuteState;
                await _bridge.SendCommandAsync(MuteState ? "MUT,ON" : "MUT,OFF");
                EnqueueDebug($"Mute {(MuteState ? "ON" : "OFF")}");
                break;

            case 'V' or 'v':
                EnqueueDebug("Volume adjust (TODO)");
                break;

            case ' ':
                SpacebarHeld = !SpacebarHeld;
                break;
        }
    }

    /// <summary>
    /// Handles key input when in Command mode.
    /// </summary>
    private async Task HandleCommandModeKeyAsync(ConsoleKeyInfo key)
    {
        // ESC - exit command mode
        if (key.Key == ConsoleKey.Escape)
        {
            ViewMode = ViewMode.Main;
            CommandInput = "";
            EnqueueDebug("Exited Command Mode");
            return;
        }

        // Enter - send command
        if (key.Key == ConsoleKey.Enter)
        {
            if (!string.IsNullOrWhiteSpace(CommandInput))
            {
                await SendCommandAsync(CommandInput);
            }
            return;
        }

        // Backspace - delete last character
        if (key.Key == ConsoleKey.Backspace)
        {
            if (CommandInput.Length > 0)
            {
                CommandInput = CommandInput[..^1];
            }
            return;
        }

        // Regular character input
        if (!char.IsControl(key.KeyChar))
        {
            CommandInput += key.KeyChar;
        }
    }

    /// <summary>
    /// Sends a command to the scanner and logs the response.
    /// </summary>
    public async Task SendCommandAsync(string command)
    {
        string timestamp = _timeProvider.Now.ToString("HH:mm:ss");
        
        // Log the sent command
        EnqueueCapped(CommandHistory, $"[{timestamp}] >> {command}", MaxCommandHistorySize);
        
        // Send and receive response
        string response = await _bridge.SendAndReceiveAsync(command, TimeSpan.FromSeconds(2));
        
        // Log the response
        string responseTimestamp = _timeProvider.Now.ToString("HH:mm:ss");
        if (response == "TIMEOUT")
        {
            EnqueueCapped(CommandHistory, $"[{responseTimestamp}] << [TIMEOUT - No response]", MaxCommandHistorySize);
        }
        else
        {
            // Split multi-line responses
            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                EnqueueCapped(CommandHistory, $"[{responseTimestamp}] << {line}", MaxCommandHistorySize);
            }
        }
        
        // Clear input for next command
        CommandInput = "";
    }

    private void EnqueueDebug(string message)
    {
        _debugLog.Enqueue($"[{_timeProvider.Now:HH:mm:ss}] {message}");
        if (_debugLog.Count > 5) _debugLog.Dequeue();
    }

    private static void EnqueueCapped(ConcurrentQueue<string> q, string item, int max)
    {
        q.Enqueue(item);
        while (q.Count > max) q.TryDequeue(out _);
    }
}
