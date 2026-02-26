using System.Collections.Concurrent;
using System.Text;
using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Logic;

/// <summary>
/// Handles GSI response accumulation, parsing, and debug log management.
/// Receives raw bytes from the bridge events, assembles complete XML documents,
/// and delegates parsing to <see cref="IResponseParser"/>.
/// </summary>
public class GsiResponseHandler
{
    private readonly StringBuilder _buffer = new();
    private readonly Queue<string> _debugLog;
    private readonly ConcurrentQueue<string> _rawRadioData;
    private readonly ScannerStatus _status;
    private readonly IResponseParser _parser;
    private readonly int _maxDebugLogSize;
    private readonly int _maxRawDataSize;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Creates a new <see cref="GsiResponseHandler"/>.
    /// </summary>
    /// <param name="status">Scanner status to update when a GSI document is parsed.</param>
    /// <param name="debugLog">Non-thread-safe queue for human-readable parsing events.</param>
    /// <param name="rawRadioData">Thread-safe queue for raw radio traffic display.</param>
    /// <param name="parser">
    /// Response parser implementation (optional; defaults to <see cref="UnidenParser.Default"/>).
    /// </param>
    /// <param name="timeProvider">Time provider for timestamps (optional; defaults to system time).</param>
    /// <param name="maxDebugLogSize">Maximum entries kept in <paramref name="debugLog"/>.</param>
    /// <param name="maxRawDataSize">Maximum entries kept in <paramref name="rawRadioData"/>.</param>
    public GsiResponseHandler(
        ScannerStatus status,
        Queue<string> debugLog,
        ConcurrentQueue<string> rawRadioData,
        IResponseParser? parser = null,
        ITimeProvider? timeProvider = null,
        int maxDebugLogSize = 5,
        int maxRawDataSize = 30)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _debugLog = debugLog ?? throw new ArgumentNullException(nameof(debugLog));
        _rawRadioData = rawRadioData ?? throw new ArgumentNullException(nameof(rawRadioData));
        _parser = parser ?? UnidenParser.Default;
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _maxDebugLogSize = maxDebugLogSize;
        _maxRawDataSize = maxRawDataSize;
    }

    /// <summary>
    /// Handles data sent to the scanner — logs with a <c>&gt;&gt;</c> prefix.
    /// </summary>
    /// <param name="data">The command string sent to the scanner.</param>
    public void OnDataSent(string data)
    {
        string entry = FormatRawEntry($">> {data}");
        EnqueueCapped(_rawRadioData, entry, _maxRawDataSize);
    }

    /// <summary>
    /// Handles data received from the scanner.
    /// Accumulates data until a complete GSI XML document is detected, then parses it.
    /// </summary>
    /// <param name="data">The raw string received from the scanner.</param>
    public void OnDataReceived(string data)
    {
        // Log the raw packet immediately for debug display
        string entry = FormatRawEntry($"<< {data}");
        EnqueueCapped(_rawRadioData, entry, _maxRawDataSize);

        // Accumulate until we have a complete XML document
        _buffer.Append(data);
        string accumulated = _buffer.ToString();

        if (accumulated.Contains("</ScannerInfo>"))
        {
            bool parsed = _parser.UpdateStatus(_status, accumulated);

            string timestamp = _timeProvider.Now.ToString("HH:mm:ss");
            EnqueueCappedDebug(parsed
                ? $"[{timestamp}] GSI parsed"
                : $"[{timestamp}] Parse failed");

            _buffer.Clear();
        }
    }

    /// <summary>Clears the internal accumulation buffer.</summary>
    public void ClearBuffer() => _buffer.Clear();

    /// <summary>Gets the current buffer contents (for debugging and testing).</summary>
    public string GetBufferContents() => _buffer.ToString();

    // ── Private helpers ────────────────────────────────────────────────

    /// <summary>Formats a raw data string with a timestamp prefix — no Presentation imports needed.</summary>
    private string FormatRawEntry(string data)
    {
        string ts = _timeProvider.Now.ToString("HH:mm:ss");
        return $"[{ts}] {data}";
    }

    private void EnqueueCappedDebug(string item)
    {
        _debugLog.Enqueue(item);
        while (_debugLog.Count > _maxDebugLogSize) _debugLog.Dequeue();
    }

    private static void EnqueueCapped(ConcurrentQueue<string> q, string item, int max)
    {
        q.Enqueue(item);
        while (q.Count > max) q.TryDequeue(out _);
    }
}
