using Spectre.Console;
using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the menu view when the scanner is in menu mode or displaying a popup.
/// </summary>
public static class MenuViewRenderer
{
    private static readonly Table _menuTable = new Table().NoBorder().HideHeaders().AddColumns("Line");

    /// <summary>
    /// Creates the fixed layout skeleton for the menu view and wires widget slots once.
    /// </summary>
    public static Layout CreateLayout()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Content"),
                new Layout("Popup").Size(5),
                new Layout("Hotkeys").Size(1),
                new Layout("Footer").Size(3)
            );

        layout["Content"].Update(new Panel(_menuTable).Expand());

        return layout;
    }

    /// <summary>
    /// Mutates the menu table rows in place and replaces immutable panel slots.
    /// </summary>
    public static void Update(Layout layout, ScannerStatus status, bool isConnected, bool spacebarHeld)
    {
        string modeDisplay = !string.IsNullOrEmpty(status.MenuTitle)
            ? status.MenuTitle
            : status.Mode;
        layout["Header"].Update(new Panel(
            new Markup($"[bold cyan]{Markup.Escape(modeDisplay)}[/]").Centered()
        ).Header(MarkupConstants.HeaderMenu).Border(BoxBorder.Double));

        // Mutate menu table rows in place
        _menuTable.Rows.Clear();
        if (status.InfoLines.Count == 0)
        {
            _menuTable.AddRow(new Markup(MarkupConstants.NoMenuInfo));
        }
        else
        {
            foreach (var line in status.InfoLines)
                _menuTable.AddRow(new Markup(FormatMenuLine(line)));
        }

        if (!string.IsNullOrEmpty(status.PopupText))
        {
            layout["Popup"].Update(new Panel(
                new Markup($"[bold yellow]{Markup.Escape(status.PopupText)}[/]").Centered()
            ).Header(MarkupConstants.HeaderPrompt).Border(BoxBorder.Rounded));
        }
        else
        {
            layout["Popup"].Update(new Panel(new Markup(MarkupConstants.NoActivePrompt)).Border(BoxBorder.None));
        }

        layout["Hotkeys"].Update(new Markup(spacebarHeld
            ? MarkupConstants.HotkeyMainExpanded
            : MarkupConstants.HotkeyMainCompact).LeftJustified());

        string connText = MarkupConstants.FormatConnectionStatus(isConnected);
        layout["Footer"].Update(new Panel(
            new Markup($"{connText}  {MarkupConstants.MenuModeIndicator}"))
            .Border(BoxBorder.None));
    }


    /// <summary>
    /// Formats a menu line with proper highlighting.
    /// Scanner uses conventions like '*' for selected items, '-' for separators.
    /// Example: "F0:01234-6*789" where * marks the selected digit
    /// </summary>
    private static string FormatMenuLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return "";

        // Escape for Spectre markup
        string escaped = Markup.Escape(line);

        // Check for selection marker '*' - highlight the character before it
        if (line.Contains('*'))
        {
            // Replace pattern like "6*" with highlighted version
            escaped = HighlightSelectedItem(escaped);
        }

        // Check if line looks like a label/value pair (contains ':')
        if (line.Contains(':'))
        {
            int colonIndex = escaped.IndexOf(':');
            if (colonIndex > 0 && colonIndex < escaped.Length - 1)
            {
                string label = escaped.Substring(0, colonIndex + 1);
                string value = escaped.Substring(colonIndex + 1);
                return $"[cyan]{label}[/][white]{value}[/]";
            }
        }

        // Check if it's a separator line (all dashes or similar)
        if (line.All(c => c == '-' || c == '=' || c == '_'))
        {
            return $"[dim]{escaped}[/]";
        }

        return escaped;
    }

    /// <summary>
    /// Highlights selected items marked with '*' in menu lines.
    /// The character immediately before '*' is the selected one.
    /// </summary>
    private static string HighlightSelectedItem(string line)
    {
        var result = new System.Text.StringBuilder();
        bool inHighlight = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            // Check if next char is '*' (selection marker)
            if (i + 1 < line.Length && line[i + 1] == '*')
            {
                // Highlight this character
                result.Append($"[bold yellow on blue]{c}[/]");
                inHighlight = true;
            }
            else if (c == '*' && inHighlight)
            {
                // Skip the '*' marker itself
                inHighlight = false;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Determines if the scanner is currently in a menu/popup state.
    /// </summary>
    public static bool IsMenuMode(ScannerStatus status)
    {
        return status.IsInMenu || !string.IsNullOrEmpty(status.PopupText);
    }
}

