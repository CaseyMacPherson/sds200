using System.Collections.Concurrent;
using SDS200.Cli.Core;
using SDS200.Cli.Presentation;

namespace SDS200.Cli.Logic;

/// <summary>
/// Handles non-blocking keyboard input polling and dispatches key actions
/// (view toggle, mute, record, volume, spacebar help).
/// </summary>
public class KeyboardHandler
{
    private readonly IScannerBridge _bridge;
    private readonly ConcurrentQueue<string> _keyboardLog;
    private readonly Queue<string> _debugLog;
    private readonly int _maxKeyboardLogSize;

    // Observable state — read by the render loop
    public ViewMode ViewMode { get; set; } = ViewMode.Main;
    public bool SpacebarHeld { get; set; }
    public bool MuteState { get; private set; }
    public bool RecordState { get; private set; }

    /// <summary>
    /// Creates a new KeyboardHandler.
    /// </summary>
    /// <param name="bridge">Scanner bridge for sending commands (MUT, REC).</param>
    /// <param name="keyboardLog">Thread-safe queue that receives timestamped key-press log entries.</param>
    /// <param name="debugLog">High-level event log (not thread-safe — only called from the keyboard task).</param>
    /// <param name="maxKeyboardLogSize">Maximum entries kept in <paramref name="keyboardLog"/>.</param>
    public KeyboardHandler(
        IScannerBridge bridge,
        ConcurrentQueue<string> keyboardLog,
        Queue<string> debugLog,
        int maxKeyboardLogSize = 10)
    {
        _bridge = bridge;
        _keyboardLog = keyboardLog;
        _debugLog = debugLog;
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
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                await HandleKeyAsync(key);
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Processes a single key press. Extracted for testability.
    /// </summary>
    public async Task HandleKeyAsync(ConsoleKeyInfo key)
    {
        string keyName = key.KeyChar switch
        {
            'D' or 'd' => MarkupConstants.KeyPressedD,
            'R' or 'r' => MarkupConstants.KeyPressedR,
            'M' or 'm' => MarkupConstants.KeyPressedM,
            'V' or 'v' => MarkupConstants.KeyPressedV,
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

    private void EnqueueDebug(string message)
    {
        _debugLog.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (_debugLog.Count > 5) _debugLog.Dequeue();
    }

    private static void EnqueueCapped(ConcurrentQueue<string> q, string item, int max)
    {
        q.Enqueue(item);
        while (q.Count > max) q.TryDequeue(out _);
    }
}
