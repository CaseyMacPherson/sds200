using System.Collections.Concurrent;
using Spectre.Console;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the command mode view for sending manual commands to the scanner.
/// </summary>
public static class CommandViewRenderer
{
    private static readonly Table _historyTable = new Table().NoBorder().HideHeaders().AddColumn("Entry").Expand();

    /// <summary>
    /// Creates the fixed layout skeleton and wires widget slots once.
    /// </summary>
    public static Layout CreateLayout()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(5),
                new Layout("History"),
                new Layout("Input").Size(3),
                new Layout("Footer").Size(1)
            );

        // Wire the static header (never changes) and persistent history table once
        var headerContent = new Rows(
            new Markup(MarkupConstants.HeaderCommand).Centered(),
            new Text(""),
            new Markup(MarkupConstants.CommandModeInstructions).Centered()
        );
        layout["Header"].Update(new Panel(headerContent).Border(BoxBorder.Double));
        layout["History"].Update(new Panel(_historyTable).Header("[bold]Command History[/]").Expand());

        return layout;
    }

    /// <summary>
    /// Mutates the history table rows in place and replaces the input/footer markup.
    /// </summary>
    public static void Update(
        Layout layout,
        bool isConnected,
        string currentInput,
        ConcurrentQueue<string> commandHistory)
    {
        // Mutate history table in place
        _historyTable.Rows.Clear();
        var historyList = commandHistory.ToArray();
        if (historyList.Length == 0)
        {
            _historyTable.AddRow(new Markup(MarkupConstants.NoCommandHistory));
        }
        else
        {
            foreach (var entry in historyList.TakeLast(30))
                _historyTable.AddRow(new Markup(FormatHistoryEntry(entry)));
        }

        string inputDisplay = $"{MarkupConstants.CommandInputPrompt}[bold white]{Markup.Escape(currentInput)}[/][blink]_[/]";
        layout["Input"].Update(new Panel(new Markup(inputDisplay))
            .Header("[bold]Input[/]")
            .Border(BoxBorder.Rounded));

        string connText = MarkupConstants.FormatConnectionStatus(isConnected);
        layout["Footer"].Update(new Markup($"{connText}  {MarkupConstants.HotkeyCommandMode}").LeftJustified());
    }


    /// <summary>
    /// Formats a history entry with appropriate colors.
    /// Sent commands are cyan, responses are green, timeouts are red.
    /// </summary>
    private static string FormatHistoryEntry(string entry)
    {
        string escaped = Markup.Escape(entry);
        
        if (entry.Contains(">>"))
        {
            // Sent command - cyan
            return $"[cyan]{escaped}[/]";
        }
        else if (entry.Contains("TIMEOUT"))
        {
            // Timeout - red
            return $"[red]{escaped}[/]";
        }
        else if (entry.Contains("<<"))
        {
            // Response - green
            return $"[green]{escaped}[/]";
        }
        
        return escaped;
    }
}
