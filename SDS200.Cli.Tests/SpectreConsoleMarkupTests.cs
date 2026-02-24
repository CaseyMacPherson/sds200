using Spectre.Console;
using Xunit;
using SDS200.Cli.Presentation;

namespace SdsRemote.Tests;

/// <summary>
/// Tests for Spectre.Console markup string handling to ensure UI rendering doesn't throw exceptions
/// and that all markup patterns used in Program.cs are properly escaped.
/// </summary>
public class SpectreConsoleMarkupTests
{
    [Fact]
    public void Markup_HotkeyTexts_AllVariants_DoNotThrow()
    {
        // Arrange - Test all hotkey constant variants
        var variants = new[]
        {
            MarkupConstants.HotkeyMainCompact,
            MarkupConstants.HotkeyMainExpanded,
            MarkupConstants.HotkeyDebugCompact,
            MarkupConstants.HotkeyDebugExpanded,
        };

        // Act & Assert - all should parse without exception
        foreach (var variant in variants)
        {
            var markup = new Markup(variant);
            Assert.NotNull(markup);
        }
    }

    [Fact]
    public void Markup_AllTableLabels_DoNotThrow()
    {
        // Arrange - All table label constants
        var labels = new[]
        {
            MarkupConstants.LabelSystem,
            MarkupConstants.LabelDepartment,
            MarkupConstants.LabelSite,
            MarkupConstants.LabelTgid,
            MarkupConstants.LabelChannel,
            MarkupConstants.LabelUnitId,
            MarkupConstants.LabelToneA,
            MarkupConstants.LabelToneB,
            MarkupConstants.LabelRange,
            MarkupConstants.LabelHits,
            MarkupConstants.LabelHold,
            MarkupConstants.LabelHoldOn,
        };

        // Act & Assert
        foreach (var label in labels)
        {
            var markup = new Markup(label);
            Assert.NotNull(markup);
        }
    }

    [Fact]
    public void Markup_PanelHeaders_AllVariants_DoNotThrow()
    {
        // Arrange
        var headers = new[]
        {
            MarkupConstants.HeaderIdentity,
            MarkupConstants.HeaderSignal,
            MarkupConstants.HeaderRecentContacts,
            MarkupConstants.HeaderDebugTraffic,
        };

        // Act & Assert
        foreach (var header in headers)
        {
            var markup = new Markup(header);
            Assert.NotNull(markup);
        }
    }

    [Fact]
    public void Markup_SignalPanelFormats_WithSampleData_DoNotThrow()
    {
        // Arrange - Test signal panel format strings with actual data
        string rssi = "S9";
        var rssiMarkup = string.Format(MarkupConstants.FormatRssi, Markup.Escape(rssi));

        int volume = 75;
        int squelch = 5;
        var volumeMarkup = string.Format(MarkupConstants.FormatVolumeSquelch, volume, squelch);

        string mute = "Unmute";
        string att = "Off";
        var muteMarkup = string.Format(MarkupConstants.FormatMuteAttenuator, Markup.Escape(mute), Markup.Escape(att));

        // Act & Assert
        Assert.NotNull(new Markup(rssiMarkup));
        Assert.NotNull(new Markup(volumeMarkup));
        Assert.NotNull(new Markup(muteMarkup));
    }

    [Fact]
    public void Markup_ConnectionStatus_Connected_DoesNotThrow()
    {
        // Arrange
        string connMarkup = MarkupConstants.FormatConnectionStatus(true);

        // Act & Assert
        var markup = new Markup(connMarkup);
        Assert.NotNull(markup);
    }

    [Fact]
    public void Markup_ConnectionStatus_Disconnected_DoesNotThrow()
    {
        // Arrange
        string connMarkup = MarkupConstants.FormatConnectionStatus(false);

        // Act & Assert
        var markup = new Markup(connMarkup);
        Assert.NotNull(markup);
    }

    [Fact]
    public void Markup_ConnectionStatusWithRecordingAndLed_DoesNotThrow()
    {
        // Arrange
        string connMarkup = MarkupConstants.FormatConnectionStatus(true);
        string recordingMarkup = MarkupConstants.RecordingIndicator;
        string ledMarkup = MarkupConstants.FormatLedIndicator(Markup.Escape("On"));
        string combined = $"{connMarkup}{recordingMarkup}{ledMarkup}";

        // Act & Assert
        var markup = new Markup(combined);
        Assert.NotNull(markup);
    }

    [Theory]
    [InlineData("conventional_scan", "SCAN")]
    [InlineData("trunk_scan", "TRUNK")]
    [InlineData("custom_with_scan", "CUSTOM/SCAN")]
    [InlineData("cchits_with_scan", "CC HITS")]
    [InlineData("custom_search", "CUSTOM SEARCH")]
    [InlineData("quick_search", "QUICK SEARCH")]
    [InlineData("close_call", "CLOSE CALL")]
    [InlineData("cc_searching", "CC SEARCH")]
    [InlineData("tone_out", "TONE OUT")]
    [InlineData("wx_alert", "WEATHER")]
    [InlineData("repeater_find", "RPT FIND")]
    [InlineData("reverse_frequency", "REVERSE")]
    [InlineData("direct_entry", "DIRECT")]
    [InlineData("discovery_conventional", "DISC CONV")]
    [InlineData("discovery_trunking", "DISC TRUNK")]
    [InlineData("analyze_system_status", "ANALYZE SYS")]
    [InlineData("analyze", "WATERFALL")]
    public void Markup_ModeLabels_AllModes_MatchExpectedLabels(string vscreen, string expectedLabel)
    {
        // Act
        string label = MarkupConstants.GetModeLabel(vscreen, "FALLBACK");

        // Assert
        Assert.Equal(expectedLabel, label);
    }

    [Fact]
    public void Markup_ModeLabels_UnknownMode_UsesFallback()
    {
        // Act
        string label = MarkupConstants.GetModeLabel("unknown_mode", "FALLBACK");

        // Assert
        Assert.Equal("FALLBACK", label);
    }

    [Fact]
    public void Markup_DebugLogConstants_AllValid()
    {
        // Arrange
        var logStrings = new[]
        {
            MarkupConstants.LogGsiReceived,
            MarkupConstants.LogParseFailed,
            MarkupConstants.LogRecordToggle,
            MarkupConstants.LogMuteToggle,
            MarkupConstants.LogVolumeAdjust,
        };

        // Act & Assert - All can be formatted into log entries
        foreach (var logMsg in logStrings)
        {
            string logEntry = MarkupConstants.FormatDebugLogEntry(logMsg);
            string display = MarkupConstants.FormatDebugLogDisplay(logEntry);
            var markup = new Markup(display);
            Assert.NotNull(markup);
        }
    }

    [Fact]
    public void Markup_MarkupPatterns_AllFormats_DoNotThrow()
    {
        // Arrange - Test format string patterns
        var formats = new Dictionary<string, object[]>
        {
            { MarkupConstants.BoldWhite, new object[] { "TestValue" } },
            { MarkupConstants.DimText, new object[] { "TestValue" } },
            { MarkupConstants.HeaderPanel, new object[] { "HEADER" } },
        };

        // Act & Assert
        foreach (var (formatStr, args) in formats)
        {
            string formatted = string.Format(formatStr, args);
            var markup = new Markup(formatted);
            Assert.NotNull(markup);
        }
    }

    [Fact]
    public void Markup_TgidDisplay_WithGuards_DoNotThrow()
    {
        // Arrange - Test TGID display with guard logic
        string[] tgidValues = { "1234", "---", "TGID" };

        foreach (var tgidVal in tgidValues)
        {
            string tgidDisplay = tgidVal != "---" && tgidVal != "TGID" ? tgidVal : "---";
            string markup = string.Format(MarkupConstants.BoldWhite, Markup.Escape(tgidDisplay));

            // Act & Assert
            Assert.NotNull(new Markup(MarkupConstants.LabelTgid));
            Assert.NotNull(new Markup(markup));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("---")]
    [InlineData("...")]
    [InlineData("N/A")]
    [InlineData("System Name With Spaces")]
    [InlineData("System@#$%")]
    public void Markup_EscapedUserData_WithDefaultValues_DoNotThrow(string userData)
    {
        // Arrange
        string escaped = Markup.Escape(userData);
        string markup = string.Format(MarkupConstants.DimText, escaped);

        // Act & Assert
        Assert.NotNull(new Markup(markup));
    }

    [Fact]
    public void Markup_IdentityTable_AllModeContexts_HaveValidLabels()
    {
        // Arrange - Verify all labels used in mode-specific identity table rows are valid
        var modeContextLabels = new Dictionary<string, string[]>
        {
            { "trunk_scan", new[] { MarkupConstants.LabelSite, MarkupConstants.LabelTgid, MarkupConstants.LabelChannel, MarkupConstants.LabelUnitId } },
            { "tone_out", new[] { MarkupConstants.LabelChannel, MarkupConstants.LabelToneA, MarkupConstants.LabelToneB } },
            { "search_modes", new[] { MarkupConstants.LabelRange, MarkupConstants.LabelHits } },
        };

        // Act & Assert
        foreach (var (mode, labels) in modeContextLabels)
        {
            foreach (var label in labels)
            {
                var markup = new Markup(label);
                Assert.NotNull(markup);
            }
        }
    }

    [Fact]
    public void Markup_AllConstants_AreNotNull()
    {
        // Assert - Verify no constant is null (static initializer check)
        Assert.NotNull(MarkupConstants.HotkeyMainCompact);
        Assert.NotNull(MarkupConstants.HotkeyMainExpanded);
        Assert.NotNull(MarkupConstants.HotkeyDebugCompact);
        Assert.NotNull(MarkupConstants.HotkeyDebugExpanded);
        Assert.NotNull(MarkupConstants.LabelSystem);
        Assert.NotNull(MarkupConstants.HeaderIdentity);
        Assert.NotNull(MarkupConstants.HeaderSignal);
        Assert.NotNull(MarkupConstants.HeaderRecentContacts);
        Assert.NotNull(MarkupConstants.HeaderDebugTraffic);
    }

    [Fact]
    public void Markup_AllConstants_AreNotEmpty()
    {
        // Assert - Verify no constant is empty string
        Assert.NotEmpty(MarkupConstants.HotkeyMainCompact);
        Assert.NotEmpty(MarkupConstants.HotkeyMainExpanded);
        Assert.NotEmpty(MarkupConstants.LabelSystem);
        Assert.NotEmpty(MarkupConstants.HeaderIdentity);
        Assert.NotEmpty(MarkupConstants.StatusConnected);
        Assert.NotEmpty(MarkupConstants.StatusDisconnected);
    }

    [Fact]
    public void Markup_HeaderPanel_FormatString_WorksWithContent()
    {
        // Arrange
        string content = "Test Header";

        // Act
        string formatted = string.Format(MarkupConstants.HeaderPanel, content);

        // Assert
        Assert.NotNull(new Markup(formatted));
        Assert.Contains("Test Header", formatted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Markup_ConnectionStatus_Functionality_ReturnsValidMarkup(bool connected)
    {
        // Act
        string result = MarkupConstants.FormatConnectionStatus(connected);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var markup = new Markup(result);
        Assert.NotNull(markup);
        
        // Verify it contains expected text
        if (connected)
            Assert.Contains(MarkupConstants.StatusConnected, result);
        else
            Assert.Contains(MarkupConstants.StatusDisconnected, result);
    }

    [Fact]
    public void Markup_LedIndicator_FormatString_WorksWithData()
    {
        // Arrange
        string ledStatus = "On";

        // Act
        string result = MarkupConstants.FormatLedIndicator(ledStatus);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ledStatus, result);
        Assert.NotNull(new Markup(result)); // Should not throw
    }

    // ── STARTUP MESSAGES ───────────────────────────────────────────────────────

    [Fact]
    public void Markup_FormatDebugModeStartup_DoesNotThrow()
    {
        // Arrange
        string connectionType = "UDP (Network)";

        // Act
        string result = MarkupConstants.FormatDebugModeStartup(connectionType);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("UDP (Network)", result);
    }

    [Fact]
    public void Markup_FormatDebugModeStartup_EscapesSpecialCharacters()
    {
        // Arrange - connection type with brackets that could be markup
        string connectionType = "[Special] Mode";

        // Act
        string result = MarkupConstants.FormatDebugModeStartup(connectionType);

        // Assert - should not throw when parsed
        Assert.NotNull(new Markup(result));
    }

    [Fact]
    public void Markup_FormatScannerIpPrompt_DoesNotThrow()
    {
        // Arrange
        string defaultIp = "192.168.1.100";

        // Act
        string result = MarkupConstants.FormatScannerIpPrompt(defaultIp);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("192.168.1.100", result);
    }

    [Fact]
    public void Markup_FormatUdpConnecting_DoesNotThrow()
    {
        // Arrange
        string ip = "10.0.0.50";

        // Act
        string result = MarkupConstants.FormatUdpConnecting(ip);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("10.0.0.50", result);
    }

    [Fact]
    public void Markup_FormatSerialConnecting_DoesNotThrow()
    {
        // Arrange
        string port = "/dev/tty.usbserial-1234";

        // Act
        string result = MarkupConstants.FormatSerialConnecting(port);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("/dev/tty.usbserial-1234", result);
    }

    [Fact]
    public void Markup_UdpConnectionConstants_DoNotThrow()
    {
        // Assert - all constants parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.UdpConnectedSuccess));
        Assert.NotNull(new Markup(MarkupConstants.UdpConnectedFailure));
    }

    [Fact]
    public void Markup_SerialConnectionConstants_DoNotThrow()
    {
        // Assert - all constants parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.SearchingForScanner));
        Assert.NotNull(new Markup(MarkupConstants.AutoDetectFailed));
        Assert.NotNull(new Markup(MarkupConstants.NoSerialPortsFound));
    }

    [Fact]
    public void Markup_FormatScannerDetected_DoesNotThrow()
    {
        // Arrange
        string port = "COM3";

        // Act
        string result = MarkupConstants.FormatScannerDetected(port);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("COM3", result);
    }

    [Fact]
    public void Markup_FormatGreyMessage_DoesNotThrow()
    {
        // Arrange
        string message = "Testing port COM1...";

        // Act
        string result = MarkupConstants.FormatGreyMessage(message);

        // Assert
        Assert.NotNull(new Markup(result));
    }

    [Fact]
    public void Markup_FormatGreyMessage_EscapesSpecialCharacters()
    {
        // Arrange - message with brackets
        string message = "Testing [special] port";

        // Act
        string result = MarkupConstants.FormatGreyMessage(message);

        // Assert - should not throw
        Assert.NotNull(new Markup(result));
    }

    // ── DEBUG VIEW PLACEHOLDERS ────────────────────────────────────────────────

    [Fact]
    public void Markup_DebugViewPlaceholders_DoNotThrow()
    {
        // Assert - both placeholders parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.NoKeyboardInput));
        Assert.NotNull(new Markup(MarkupConstants.NoRadioData));
    }

    [Fact]
    public void Markup_FormatModulation_DoesNotThrow()
    {
        // Arrange
        string modulation = "P25";

        // Act
        string result = MarkupConstants.FormatModulation(modulation);

        // Assert
        Assert.NotNull(new Markup(result));
        Assert.Contains("P25", result);
    }

    [Fact]
    public void Markup_FormatModulation_EscapesSpecialCharacters()
    {
        // Arrange - modulation with brackets
        string modulation = "[P25] Phase 1";

        // Act
        string result = MarkupConstants.FormatModulation(modulation);

        // Assert - should not throw
        Assert.NotNull(new Markup(result));
    }

    // ── STATUS INDICATORS ──────────────────────────────────────────────────────

    [Fact]
    public void Markup_StatusIndicators_DoNotThrow()
    {
        // Assert - all status indicators parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.StatusDataUnrecognized));
        Assert.NotNull(new Markup(MarkupConstants.StatusUpdated));
        Assert.NotNull(new Markup(MarkupConstants.StatusTimeout));
    }

    [Fact]
    public void Markup_StatusIndicators_ContainExpectedText()
    {
        // Assert - status indicators have descriptive text
        Assert.Contains("WARN", MarkupConstants.StatusDataUnrecognized);
        Assert.Contains("OK", MarkupConstants.StatusUpdated);
        Assert.Contains("TIMEOUT", MarkupConstants.StatusTimeout);
    }

    [Fact]
    public void Markup_StatusIndicators_DoNotContainEmoji()
    {
        // Assert - ensure emojis have been removed
        Assert.DoesNotContain("✅", MarkupConstants.StatusUpdated);
        Assert.DoesNotContain("⚠️", MarkupConstants.StatusDataUnrecognized);
        Assert.DoesNotContain("⌛", MarkupConstants.StatusTimeout);
    }

    // ── MENU VIEW ──────────────────────────────────────────────────────────────

    [Fact]
    public void Markup_MenuViewConstants_DoNotThrow()
    {
        // Assert - all menu view constants parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.HeaderMenu));
        Assert.NotNull(new Markup(MarkupConstants.HeaderPrompt));
        Assert.NotNull(new Markup(MarkupConstants.MenuModeIndicator));
        Assert.NotNull(new Markup(MarkupConstants.NoMenuInfo));
        Assert.NotNull(new Markup(MarkupConstants.NoActivePrompt));
    }

    // ── COMMAND VIEW ───────────────────────────────────────────────────────────

    [Fact]
    public void Markup_CommandViewConstants_DoNotThrow()
    {
        // Assert - all command view constants parse as valid markup
        Assert.NotNull(new Markup(MarkupConstants.HeaderCommand));
        Assert.NotNull(new Markup(MarkupConstants.CommandModeInstructions));
        Assert.NotNull(new Markup(MarkupConstants.CommandInputPrompt));
        Assert.NotNull(new Markup(MarkupConstants.NoCommandHistory));
        Assert.NotNull(new Markup(MarkupConstants.CommandModeIndicator));
        Assert.NotNull(new Markup(MarkupConstants.HotkeyCommandMode));
    }

    [Fact]
    public void Markup_NewKeyConstants_DoNotThrow()
    {
        // Assert - C and Q key constants parse correctly
        Assert.NotNull(MarkupConstants.KeyPressedC);
        Assert.NotNull(MarkupConstants.KeyPressedQ);
        Assert.Contains("Command", MarkupConstants.KeyPressedC);
        Assert.Contains("Quit", MarkupConstants.KeyPressedQ);
    }

    [Fact]
    public void Markup_HotkeyDisplays_IncludeCommandAndQuit()
    {
        // Assert - hotkey displays include the new C and Q keys
        Assert.Contains("C", MarkupConstants.HotkeyMainCompact);
        Assert.Contains("Q", MarkupConstants.HotkeyMainCompact);
        Assert.Contains("Command", MarkupConstants.HotkeyMainExpanded);
        Assert.Contains("Quit", MarkupConstants.HotkeyMainExpanded);
    }
}
