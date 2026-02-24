using System.Collections.Concurrent;
using Spectre.Console;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the command mode view for sending manual commands to the scanner.
/// </summary>
public static class CommandViewRenderer
{
    /// <summary>
    /// Renders the command view layout.
    /// </summary>
    /// <param name="isConnected">Whether the scanner is connected.</param>
    /// <param name="currentInput">Current command being typed.</param>
    /// <param name="commandHistory">History of sent commands and responses.</param>
    /// <returns>A Layout containing the command view.</returns>
    public static Layout Render(
        bool isConnected,
        string currentInput,
        ConcurrentQueue<string> commandHistory)
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(5),
                new Layout("History"),
                new Layout("Input").Size(3),
                new Layout("Footer").Size(1)
            );

        // Header - title and instructions
        var headerContent = new Rows(
            new Markup(MarkupConstants.HeaderCommand).Centered(),
            new Text(""),
            new Markup(MarkupConstants.CommandModeInstructions).Centered()
        );
        layout["Header"].Update(new Panel(headerContent).Border(BoxBorder.Double));

        // History - command/response log
        var historyTable = BuildHistoryTable(commandHistory);
        layout["History"].Update(new Panel(historyTable)
            .Header("[bold]Command History[/]")
            .Expand());

        // Input - current command being typed
        string inputDisplay = $"{MarkupConstants.CommandInputPrompt}[bold white]{Markup.Escape(currentInput)}[/][blink]_[/]";
        layout["Input"].Update(new Panel(new Markup(inputDisplay))
            .Header("[bold]Input[/]")
            .Border(BoxBorder.Rounded));

        // Footer - connection status and hotkey hint
        string connText = MarkupConstants.FormatConnectionStatus(isConnected);
        layout["Footer"].Update(new Markup($"{connText}  {MarkupConstants.HotkeyCommandMode}").LeftJustified());

        return layout;
    }

    private static Table BuildHistoryTable(ConcurrentQueue<string> commandHistory)
    {
        var table = new Table().NoBorder().HideHeaders().AddColumn("Entry").Expand();
        
        var historyList = commandHistory.ToArray();
        
        if (historyList.Length == 0)
        {
            table.AddRow(new Markup(MarkupConstants.NoCommandHistory));
            return table;
        }

        // Show history with color coding - reverse to show newest at bottom (natural scroll)
        // Take last entries that fit in the view (approximately)
        var recentHistory = historyList.TakeLast(30).ToArray();
        
        foreach (var entry in recentHistory)
        {
            string formattedEntry = FormatHistoryEntry(entry);
            table.AddRow(new Markup(formattedEntry));
        }

        return table;
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
