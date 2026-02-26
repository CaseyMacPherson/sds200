using Spectre.Console;
using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Renders the main scanning view with frequency display, identity info, and contacts.
/// All layout slots are wired once in CreateLayout(). Update() only mutates table rows
/// and markup text in place — no new widget objects are created per frame.
/// </summary>
public static class MainViewRenderer
{
    // All persistent widgets — created once, mutated each frame
    private static readonly Table _identityTable = new Table().NoBorder().HideHeaders().AddColumns("L", "V");
    private static readonly Table _contactTable  = new Table().NoBorder().HideHeaders().AddColumns("Freq", "Mode", "ID", "Dur");
    private static readonly Table _rssiTable     = new Table().NoBorder().HideHeaders().AddColumns("V");
    private static readonly Table _heroTable     = new Table().NoBorder().HideHeaders().AddColumns("V");
    private static readonly Table _hotkeysTable  = new Table().NoBorder().HideHeaders().AddColumns("V");
    private static readonly Table _footerTable   = new Table().NoBorder().HideHeaders().AddColumns("V");

    // Track last values to avoid replacing Panel objects unless the header text actually changed
    private static string _lastModeLabel = string.Empty;

    /// <summary>
    /// Creates the fixed layout skeleton and wires all widget slots once.
    /// </summary>
    public static Layout CreateLayout()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Hero").Size(11),
                new Layout("Mid").Size(9).SplitColumns(new Layout("Data"), new Layout("RSSI")),
                new Layout("Contacts").Size(6),
                new Layout("Hotkeys").Size(1),
                new Layout("Footer").Size(3)
            );

        layout["Hero"].Update(new Panel(_heroTable).Border(BoxBorder.Double));
        layout["Data"].Update(new Panel(_identityTable).Header(MarkupConstants.HeaderIdentity).Expand());
        layout["RSSI"].Update(new Panel(Align.Center(_rssiTable, VerticalAlignment.Middle)).Header(MarkupConstants.HeaderSignal));
        layout["Contacts"].Update(new Panel(_contactTable).Header(MarkupConstants.HeaderRecentContacts).Expand());
        layout["Hotkeys"].Update(new Panel(_hotkeysTable).Border(BoxBorder.None));
        layout["Footer"].Update(new Panel(_footerTable).Border(BoxBorder.None));

        return layout;
    }

    /// <summary>
    /// Mutates all persistent table rows in place — no new widget objects created.
    /// </summary>
    public static void Update(
        Layout layout,
        ScannerStatus status,
        bool isConnected,
        IEnumerable<ContactLogEntry> contacts,
        bool spacebarHeld)
    {
        // Hero: only replace Panel wrapper when header text changes (mode label is stable during scanning)
        string modeLabel = MarkupConstants.GetModeLabel(status.VScreen, status.Mode);
        if (modeLabel != _lastModeLabel)
        {
            _lastModeLabel = modeLabel;
            layout["Hero"].Update(new Panel(_heroTable)
                .Header(string.Format(MarkupConstants.HeaderPanel, Markup.Escape(modeLabel)))
                .Border(BoxBorder.Double));
        }
        _heroTable.Rows.Clear();
        _heroTable.AddRow(new FigletText($"{status.Frequency:F4} MHz").Color(Color.Green).Centered());
        _heroTable.AddRow(new Markup(MarkupConstants.FormatModulation(status.Modulation)).Centered());

        // Identity table
        _identityTable.Rows.Clear();
        _identityTable.AddRow(MarkupConstants.LabelSystem, Markup.Escape(status.SystemName));
        _identityTable.AddRow(MarkupConstants.LabelDepartment, Markup.Escape(status.DepartmentName));
        AddIdentityRows(_identityTable, status);
        if (status.Hold == "On")
            _identityTable.AddRow(MarkupConstants.LabelHold, MarkupConstants.LabelHoldOn);

        // RSSI table
        _rssiTable.Rows.Clear();
        _rssiTable.AddRow(new Markup(string.Format(MarkupConstants.FormatRssi, Markup.Escape(status.Rssi))));
        _rssiTable.AddRow(new Markup(string.Format(MarkupConstants.FormatVolumeSquelch, status.Volume, status.Squelch)));
        _rssiTable.AddRow(new Markup(string.Format(MarkupConstants.FormatMuteAttenuator, Markup.Escape(status.Mute), Markup.Escape(status.Attenuator))));

        // Contact table
        _contactTable.Rows.Clear();
        int count = 0;
        foreach (var contact in contacts)
        {
            if (count >= 5) break;
            string contactId = contact.TgId != "---" ? contact.TgId : contact.ChannelName;
            _contactTable.AddRow(
                $"{contact.Frequency:F4}",
                contact.Mode,
                Markup.Escape(contactId),
                $"{(int)contact.DurationSeconds}s"
            );
            count++;
        }

        // Hotkeys
        _hotkeysTable.Rows.Clear();
        _hotkeysTable.AddRow(new Markup(spacebarHeld
            ? MarkupConstants.HotkeyMainExpanded
            : MarkupConstants.HotkeyMainCompact));

        // Footer
        string connText = MarkupConstants.FormatConnectionStatus(isConnected);
        string statusExtra = status.Recording == "On" ? $"  {MarkupConstants.RecordingIndicator}" : "";
        string ledExtra = status.AlertLed != "Off" ? MarkupConstants.FormatLedIndicator(Markup.Escape(status.AlertLed)) : "";
        _footerTable.Rows.Clear();
        _footerTable.AddRow(new Markup($"{connText}{statusExtra}{ledExtra}"));
    }

    private static void AddIdentityRows(Table info, ScannerStatus s)
    {
        switch (s.VScreen)
        {
            case "trunk_scan":
                info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
                string tgidTrunk = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
                info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidTrunk)));
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                if (s.UnitId != "---") info.AddRow(MarkupConstants.LabelUnitId, Markup.Escape(s.UnitId));
                break;

            case "tone_out":
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                info.AddRow(MarkupConstants.LabelToneA, Markup.Escape(s.ToneA));
                info.AddRow(MarkupConstants.LabelToneB, Markup.Escape(s.ToneB));
                break;

            case "custom_search":
            case "quick_search":
            case "close_call":
            case "cc_searching":
            case "repeater_find":
            case "reverse_frequency":
            case "direct_entry":
                if (s.SearchRangeLower != "---")
                    info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
                break;

            case "discovery_conventional":
                if (s.SearchRangeLower != "---")
                    info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
                if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
                break;

            case "discovery_trunking":
                info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
                string tgidDisc = s.TgId != "---" && s.TgId != "TGID" ? s.TgId : "---";
                info.AddRow(MarkupConstants.LabelTgid, string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisc)));
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                if (s.HitCount > 0) info.AddRow(MarkupConstants.LabelHits, s.HitCount.ToString());
                break;

            case "analyze_system_status":
                info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
                break;

            case "analyze":
                info.AddRow(MarkupConstants.LabelSite, Markup.Escape(s.SiteName));
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                if (s.SearchRangeLower != "---")
                    info.AddRow(MarkupConstants.LabelRange, $"{Markup.Escape(s.SearchRangeLower)} - {Markup.Escape(s.SearchRangeUpper)}");
                break;

            default:
                info.AddRow(MarkupConstants.LabelChannel, string.Format(MarkupConstants.BoldWhite, Markup.Escape(s.ChannelName)));
                break;
        }
    }
}
