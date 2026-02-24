using System.Collections.Concurrent;
using System.Text;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Presentation;
using Spectre.Console;

namespace SDS200.Cli.Logic;

/// <summary>
/// Handles GSI response accumulation, parsing, and debug log management.
/// Consolidates duplicated logic from UDP and Serial event handlers.
/// </summary>
public class GsiResponseHandler
{
    private readonly StringBuilder _buffer = new();
    private readonly Queue<string> _debugLog;
    private readonly ConcurrentQueue<string> _rawRadioData;
    private readonly ScannerStatus _status;
    private readonly int _maxDebugLogSize;
    private readonly int _maxRawDataSize;

    /// <summary>
    /// Creates a new GsiResponseHandler.
    /// </summary>
    /// <param name="status">Scanner status to update when GSI is parsed.</param>
    /// <param name="debugLog">Debug log queue for parsing events.</param>
    /// <param name="rawRadioData">Thread-safe queue for raw radio data display.</param>
    /// <param name="maxDebugLogSize">Maximum entries in debug log (default 5).</param>
    /// <param name="maxRawDataSize">Maximum entries in raw data log (default 30).</param>
    public GsiResponseHandler(
        ScannerStatus status,
        Queue<string> debugLog,
        ConcurrentQueue<string> rawRadioData,
        int maxDebugLogSize = 5,
        int maxRawDataSize = 30)
    {
        _status = status;
        _debugLog = debugLog;
        _rawRadioData = rawRadioData;
        _maxDebugLogSize = maxDebugLogSize;
        _maxRawDataSize = maxRawDataSize;
    }

    /// <summary>
    /// Handles data sent to the scanner (logs with >> prefix).
    /// </summary>
    /// <param name="data">The command sent to the scanner.</param>
    public void OnDataSent(string data)
    {
        EnqueueCapped(_rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($">> {data}"), _maxRawDataSize);
    }

    /// <summary>
    /// Handles data received from the scanner.
    /// Accumulates data until a complete GSI response is received, then parses it.
    /// </summary>
    /// <param name="data">The data received from the scanner.</param>
    public void OnDataReceived(string data)
    {
        // Store raw data for debug display with << prefix
        EnqueueCapped(_rawRadioData, DebugDisplayFactory.CreateRadioDataEntry($"<< {data}"), _maxRawDataSize);

        // Accumulate incoming data until we have a complete GSI response
        _buffer.Append(data);
        string accumulated = _buffer.ToString();

        // Only attempt parsing when we have a complete document
        if (accumulated.Contains("</ScannerInfo>"))
        {
            var safeData = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] GSI parsed");
            EnqueueCappedDebug(safeData);

            bool parsed = UnidenParser.UpdateStatus(_status, accumulated);
            if (!parsed)
            {
                var errorLog = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] Parse failed");
                EnqueueCappedDebug(errorLog);
            }

            // Reset buffer after processing
            _buffer.Clear();
        }
    }

    /// <summary>
    /// Clears the internal buffer. Useful when resetting state.
    /// </summary>
    public void ClearBuffer()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Gets the current buffer contents (for debugging/testing).
    /// </summary>
    public string GetBufferContents() => _buffer.ToString();

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

