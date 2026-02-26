using Spectre.Console;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Factory for constructing debug display strings with proper Spectre.Console markup handling.
/// Ensures consistent string formatting between Program.cs and tests.
/// </summary>
public static class DebugDisplayFactory
{
    /// <summary>
    /// Creates a keyboard input log entry with timestamp and key name.
    /// Format: "[HH:mm:ss] KEY PRESSED: {keyName}"
    /// </summary>
    /// <param name="keyName">The key description (e.g., "D (Toggle Debug View)")</param>
    /// <param name="timestamp">Timestamp obtained from <c>ITimeProvider</c>.</param>
    public static string CreateKeyboardLogEntry(string keyName, DateTime? timestamp = null)
    {
        var ts = (timestamp ?? DateTime.Now).ToString("HH:mm:ss");
        return $"[{ts}] KEY PRESSED: {keyName}";
    }

    /// <summary>
    /// Creates a radio data log entry with timestamp and received data.
    /// Format: "[HH:mm:ss] {data}"
    /// </summary>
    /// <param name="data">The raw radio data received (GSI response)</param>
    /// <param name="timestamp">Timestamp obtained from <c>ITimeProvider</c>.</param>
    public static string CreateRadioDataEntry(string data, DateTime? timestamp = null)
    {
        var ts = (timestamp ?? DateTime.Now).ToString("HH:mm:ss");
        return $"[{ts}] {data}";
    }

    /// <summary>
    /// Creates the debug status line with connection status, packet count, and timestamp.
    /// Includes Spectre.Console markup for colors: [green]CONNECTED[/], [yellow]count[/], [cyan]timestamp[/]
    /// </summary>
    /// <param name="connected">Whether the scanner is currently connected</param>
    /// <param name="packetCount">Number of radio data packets received</param>
    /// <param name="timestamp">Timestamp obtained from <c>ITimeProvider</c>.</param>
    public static string CreateStatusLine(bool connected, int packetCount, DateTime? timestamp = null)
    {
        var ts = (timestamp ?? DateTime.Now).ToString("HH:mm:ss");
        string connStatus = connected ? "[green]CONNECTED[/]" : "[red]DISCONNECTED[/]";
        return $"{connStatus}  |  Radio Data Packets: [yellow]{packetCount}[/]  |  Last Updated: [cyan]{ts}[/]";
    }

    /// <summary>
    /// Escapes a string for safe display in Spectre.Console markup context.
    /// Converts special characters like [, ], &lt;, &gt;, &amp; to their escaped forms.
    /// </summary>
    /// <param name="raw">The raw string that may contain markup-sensitive characters</param>
    public static string EscapeForDisplay(string raw)
    {
        return Markup.Escape(raw);
    }
}
