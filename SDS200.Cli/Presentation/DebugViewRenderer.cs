using System.Collections.Concurrent;
using Spectre.Console;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the debug view showing raw radio traffic and keyboard input.
/// Follows the Spectre.Console Live pattern: persistent Table mutated via
/// Rows.Clear() + AddRow() each frame rather than creating new widget objects.
/// </summary>
public static class DebugViewRenderer
{
    private static readonly Table _debugTable = new Table().NoBorder().HideHeaders()
        .AddColumns("Keyboard Input", "Raw Radio Traffic");

    /// <summary>
    /// Creates the fixed layout skeleton and wires all widget slots once.
    /// </summary>
    public static Layout CreateLayout()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Status").Size(3),
                new Layout("Data"),
                new Layout("Footer").Size(1)
            );

        layout["Data"].Update(new Panel(_debugTable).Expand());

        return layout;
    }

    /// <summary>
    /// Mutates the debug table rows in place and replaces the status/footer markup.
    /// </summary>
    public static void Update(
        Layout layout,
        bool isConnected,
        ConcurrentQueue<string> rawRadioData,
        ConcurrentQueue<string> keyboardInputLog,
        bool spacebarHeld)
    {
        layout["Status"].Update(new Panel(
            new Markup(DebugDisplayFactory.CreateStatusLine(isConnected, rawRadioData.Count)).LeftJustified()
        ).Border(BoxBorder.Rounded));

        var keyboardList = keyboardInputLog.ToArray();
        var radioList = rawRadioData.ToArray();
        int maxRows = Math.Max(keyboardList.Length, radioList.Length);

        _debugTable.Rows.Clear();

        if (maxRows == 0)
        {
            _debugTable.AddRow(
                new Markup(MarkupConstants.NoKeyboardInput),
                new Markup(MarkupConstants.NoRadioData));
        }
        else
        {
            for (int i = 0; i < maxRows; i++)
            {
                string keyInput = i < keyboardList.Length
                    ? DebugDisplayFactory.EscapeForDisplay(keyboardList[i]) : "";
                string radioData = i < radioList.Length
                    ? DebugDisplayFactory.EscapeForDisplay(radioList[i]) : "";
                _debugTable.AddRow(new Markup(keyInput), new Markup(radioData));
            }
        }

        layout["Footer"].Update(new Markup(spacebarHeld
            ? MarkupConstants.HotkeyDebugExpanded
            : MarkupConstants.HotkeyDebugCompact).LeftJustified());
    }
}
