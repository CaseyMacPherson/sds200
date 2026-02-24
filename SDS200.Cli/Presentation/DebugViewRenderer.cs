using System.Collections.Concurrent;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the debug view showing raw radio traffic and keyboard input.
/// </summary>
public static class DebugViewRenderer
{
    /// <summary>
    /// Renders the debug view layout.
    /// </summary>
    /// <param name="isConnected">Whether the scanner is connected.</param>
    /// <param name="rawRadioData">Thread-safe queue of raw radio data entries.</param>
    /// <param name="keyboardInputLog">Thread-safe queue of keyboard input entries.</param>
    /// <param name="spacebarHeld">Whether the spacebar is being held (for expanded hotkeys).</param>
    /// <returns>A Layout containing the debug view.</returns>
    public static Layout Render(
        bool isConnected,
        ConcurrentQueue<string> rawRadioData,
        ConcurrentQueue<string> keyboardInputLog,
        bool spacebarHeld)
    {
        // Status header showing connection and data counts
        string statusLine = DebugDisplayFactory.CreateStatusLine(isConnected, rawRadioData.Count);

        // Build table with raw radio data and keyboard input
        var debugTable = new Table().NoBorder().HideHeaders().AddColumns("Keyboard Input", "Raw Radio Traffic");

        // Snapshot both queues (thread-safe ToArray on ConcurrentQueue)
        var keyboardList = keyboardInputLog.ToArray();
        var radioList = rawRadioData.ToArray();
        int maxRows = Math.Max(keyboardList.Length, radioList.Length);

        if (maxRows == 0)
        {
            debugTable.AddRow(
                new Markup(MarkupConstants.NoKeyboardInput),
                new Markup(MarkupConstants.NoRadioData)
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

        string hotkeyTxt = spacebarHeld
            ? MarkupConstants.HotkeyDebugExpanded
            : MarkupConstants.HotkeyDebugCompact;
        debugLayout["Footer"].Update(new Markup(hotkeyTxt).LeftJustified());

        return debugLayout;
    }
}

