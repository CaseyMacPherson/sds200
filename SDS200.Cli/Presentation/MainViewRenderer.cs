using Spectre.Console;
using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the main scanning view with frequency display, identity info, and contacts.
/// </summary>
public static class MainViewRenderer
{
    /// <summary>
    /// Renders the main view layout.
    /// </summary>
    /// <param name="status">Current scanner status.</param>
    /// <param name="isConnected">Whether the scanner is connected.</param>
    /// <param name="contacts">Recent contact log entries.</param>
    /// <param name="spacebarHeld">Whether the spacebar is being held (for expanded hotkeys).</param>
    /// <returns>A Layout containing the main view.</returns>
    public static Layout Render(
        ScannerStatus status,
        bool isConnected,
        IEnumerable<ContactLogEntry> contacts,
        bool spacebarHeld)
    {
        // Build mode-aware identity table
        var info = BuildIdentityTable(status);

        // Contact log table (show recent contacts)
        var contactTable = BuildContactTable(contacts);

        // Mode label for the hero panel header
        string modeLabel = MarkupConstants.GetModeLabel(status.VScreen, status.Mode);

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Hero"),
                new Layout("Mid").SplitColumns(new Layout("Data"), new Layout("RSSI")),
                new Layout("Contacts").Size(6),
                new Layout("Hotkeys").Size(1),
                new Layout("Footer").Size(3)
            );

        // Hero panel with frequency
        var freqFiglet = new FigletText($"{status.Frequency:F4} MHz")
            .Color(Color.Green)
            .Centered();
        var heroContent = new Rows(
            freqFiglet,
            new Markup(MarkupConstants.FormatModulation(status.Modulation)).Centered()
        );
        layout["Hero"].Update(new Panel(heroContent)
            .Header(string.Format(MarkupConstants.HeaderPanel, Markup.Escape(modeLabel)))
            .Border(BoxBorder.Double));

        // Data panel
        layout["Data"].Update(new Panel(info).Header(MarkupConstants.HeaderIdentity).Expand());

        // Signal panel
        var signalRows = new Rows(
            new Markup(string.Format(MarkupConstants.FormatRssi, Markup.Escape(status.Rssi))),
            new Markup(string.Format(MarkupConstants.FormatVolumeSquelch, status.Volume, status.Squelch)),
            new Markup(string.Format(MarkupConstants.FormatMuteAttenuator, Markup.Escape(status.Mute), Markup.Escape(status.Attenuator)))
        );
        layout["RSSI"].Update(new Panel(
            Align.Center(signalRows, VerticalAlignment.Middle))
            .Header(MarkupConstants.HeaderSignal));

        // Contact log panel
        layout["Contacts"].Update(new Panel(contactTable).Header(MarkupConstants.HeaderRecentContacts).Expand());

        // Hotkey panel
        string hotkeyText = spacebarHeld
            ? MarkupConstants.HotkeyMainExpanded
            : MarkupConstants.HotkeyMainCompact;
        layout["Hotkeys"].Update(new Markup(hotkeyText).LeftJustified());

        // Footer with connection status
        string connText = MarkupConstants.FormatConnectionStatus(isConnected);
        string statusExtra = status.Recording == "On" ? $"  {MarkupConstants.RecordingIndicator}" : "";
        string ledExtra = status.AlertLed != "Off" ? MarkupConstants.FormatLedIndicator(Markup.Escape(status.AlertLed)) : "";
        layout["Footer"].Update(new Panel(
            new Markup($"{connText}{statusExtra}{ledExtra}"))
            .Border(BoxBorder.None));

        return layout;
    }

    private static Table BuildIdentityTable(ScannerStatus s)
    {
        var info = new Table().NoBorder().HideHeaders().AddColumns("L", "V")
            .AddRow(MarkupConstants.LabelSystem, Markup.Escape(s.SystemName))
            .AddRow(MarkupConstants.LabelDepartment, Markup.Escape(s.DepartmentName));

        // Show context-appropriate rows based on V_Screen
        switch (s.VScreen)
        {
            case "trunk_scan":
                AddTrunkScanRows(info, s);
                break;

            case "tone_out":
                AddToneOutRows(info, s);
                break;

            case "custom_search":
            case "quick_search":
            case "close_call":
            case "cc_searching":
            case "repeater_find":
            case "reverse_frequency":
            case "direct_entry":
                AddSearchRows(info, s);
                break;

            case "discovery_conventional":
                AddDiscoveryConventionalRows(info, s);
                break;

            case "discovery_trunking":
                AddDiscoveryTrunkingRows(info, s);
                break;

            case "analyze_system_status":
                info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
                break;

            case "analyze":
                AddAnalyzeRows(info, s);
                break;

            default: // conventional_scan, custom_with_scan, cchits_with_scan, wx_alert, etc.
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                break;
        }

        // Hold indicator
        if (s.Hold == "On")
            info.AddRow(MarkupConstants.LabelHold, MarkupConstants.LabelHoldOn);

        return info;
    }

    private static void AddTrunkScanRows(Table info, ScannerStatus s)
    {
        info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
        string tgidDisplay = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
        info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisplay)));
        info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
        if (s.UnitId != "---") info.AddRow(MarkupConstants.LabelUnitId, Markup.Escape(s.UnitId));
    }

    private static void AddToneOutRows(Table info, ScannerStatus s)
    {
        info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
        info.AddRow(MarkupConstants.LabelToneA, Markup.Escape(s.ToneA));
        info.AddRow(MarkupConstants.LabelToneB, Markup.Escape(s.ToneB));
    }

    private static void AddSearchRows(Table info, ScannerStatus s)
    {
        if (s.SearchRangeLower != "---")
            info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
    }

    private static void AddDiscoveryConventionalRows(Table info, ScannerStatus s)
    {
        if (s.SearchRangeLower != "---")
            info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
        if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
    }

    private static void AddDiscoveryTrunkingRows(Table info, ScannerStatus s)
    {
        info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
        string tgidDisplay = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
        info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisplay)));
        info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
        if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
    }

    private static void AddAnalyzeRows(Table info, ScannerStatus s)
    {
        info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
        info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
        if (s.SearchRangeLower != "---")
            info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
    }

    private static Table BuildContactTable(IEnumerable<ContactLogEntry> contacts)
    {
        var contactTable = new Table().NoBorder().HideHeaders().AddColumns("Freq", "Mode", "ID", "Dur");
        int contactCount = 0;
        foreach (var contact in contacts)
        {
            if (contactCount >= 5) break; // Show only last 5
            string contactId = contact.TgId != "---" ? contact.TgId : contact.ChannelName;
            int durSec = (int)contact.DurationSeconds;
            contactTable.AddRow(
                $"{contact.Frequency:F4}",
                contact.Mode,
                Markup.Escape(contactId),
                $"{durSec}s"
            );
            contactCount++;
        }
        return contactTable;
    }
}

