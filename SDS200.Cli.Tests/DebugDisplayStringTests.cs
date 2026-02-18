using Spectre.Console;
using Xunit;
using SDS200.Cli.Presentation;

namespace SdsRemote.Tests;

/// <summary>
/// Tests for DebugDisplayFactory - verifies that debug display strings are constructed 
/// consistently and can be safely rendered in Spectre.Console without markup errors.
/// Both Program.cs and these tests use the same factory methods.
/// </summary>
public class DebugDisplayStringTests
{
    [Fact]
    public void CreateKeyboardLogEntry_WithDebugKeyConstant_ContainsExpectedFormat()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        string keyName = MarkupConstants.KeyPressedD;
        
        // Act
        string result = DebugDisplayFactory.CreateKeyboardLogEntry(keyName, timestamp);

        // Assert
        Assert.StartsWith("[12:34:56]", result);
        Assert.Contains("KEY PRESSED:", result);
        Assert.Contains(keyName, result);
        Assert.Equal("[12:34:56] KEY PRESSED: D (Toggle Debug View)", result);
    }

    [Theory]
    [InlineData("D (Toggle Debug View)")]
    [InlineData("R (Record Toggle)")]
    [InlineData("M (Mute Toggle)")]
    [InlineData("V (Volume)")]
    [InlineData("SPACE (Hotkey Help)")]
    public void CreateKeyboardLogEntry_WithAllKeyTypes_CanBeEscapedAndDisplayed(string keyName)
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        
        // Act - Create entry using factory
        string entry = DebugDisplayFactory.CreateKeyboardLogEntry(keyName, timestamp);
        
        // Escape for safe display
        string escaped = DebugDisplayFactory.EscapeForDisplay(entry);
        
        // Create markup - should not throw
        var markup = new Markup(escaped);

        // Assert
        Assert.NotNull(markup);
        Assert.StartsWith("[12:34:56]", entry);
    }

    [Fact]
    public void CreateRadioDataEntry_WithTypicalGsiResponse_ContainsExpectedFormat()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        string data = "GSI,<XML>,<?xml version=\"1.0\"?><ScannerInfo V_Screen=\"trunk_scan\"><System>Police</System></ScannerInfo>";
        
        // Act
        string result = DebugDisplayFactory.CreateRadioDataEntry(data, timestamp);

        // Assert
        Assert.StartsWith("[12:34:56]", result);
        Assert.Contains("GSI,<XML>", result);
        Assert.Contains("<ScannerInfo", result);
    }

    [Fact]
    public void CreateRadioDataEntry_WhenEscaped_CanBeDisplayedInMarkup()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        string data = "GSI,<XML>,<?xml version=\"1.0\"?><ScannerInfo V_Screen=\"trunk_scan\"><System>Police</System></ScannerInfo>";
        
        // Act
        string entry = DebugDisplayFactory.CreateRadioDataEntry(data, timestamp);
        string escaped = DebugDisplayFactory.EscapeForDisplay(entry);
        var markup = new Markup(escaped);

        // Assert - Markup.Escape escapes brackets that could be markup tags
        Assert.NotNull(markup);
        // The entry should have been escaped (different from original)
        Assert.NotEqual(entry, escaped); 
        // And should be safe to display in Markup without throwing
    }

    [Fact]
    public void CreateStatusLine_WhenConnected_ContainsGreenConnectedMarkup()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        int packetCount = 15;
        bool connected = true;
        
        // Act
        string result = DebugDisplayFactory.CreateStatusLine(connected, packetCount, timestamp);

        // Assert
        Assert.Contains("[green]CONNECTED[/]", result);
        Assert.Contains("[yellow]15[/]", result);
        Assert.Contains("[cyan]12:34:56[/]", result);
    }

    [Fact]
    public void CreateStatusLine_WhenDisconnected_ContainsRedDisconnectedMarkup()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        int packetCount = 0;
        bool connected = false;
        
        // Act
        string result = DebugDisplayFactory.CreateStatusLine(connected, packetCount, timestamp);

        // Assert
        Assert.Contains("[red]DISCONNECTED[/]", result);
        Assert.Contains("[yellow]0[/]", result);
        Assert.Contains("[cyan]12:34:56[/]", result);
    }

    [Fact]
    public void CreateStatusLine_WithValidMarkup_CanBeDisplayedInPanel()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        
        // Act
        string line = DebugDisplayFactory.CreateStatusLine(true, 42, timestamp);
        var markup = new Markup(line);

        // Assert
        Assert.NotNull(markup);
    }

    [Fact]
    public void EscapeForDisplay_WithTimestampInBrackets_EscapesCorrectly()
    {
        // Arrange
        string raw = "[12:34:56] Some data";

        // Act
        string escaped = DebugDisplayFactory.EscapeForDisplay(raw);

        // Assert - Markup.Escape doubles brackets: [  becomes [ [
        Assert.NotEqual(raw, escaped); // Should be different
        // The escaped version should be safe to put in Markup without throwing
        var markup = new Markup(escaped);
        Assert.NotNull(markup);
        // Verify brackets were escaped by counting them
        Assert.True(escaped.Contains("[[") || escaped.Contains("]]"));
    }

    [Fact]
    public void EscapeForDisplay_WithXmlSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        string raw = "[12:34:56] <ScannerInfo>Data & more</ScannerInfo>";

        // Act
        string escaped = DebugDisplayFactory.EscapeForDisplay(raw);

        // Assert - Markup.Escape handles brackets that could be Spectre markup tags
        Assert.NotEqual(raw, escaped); // Should be modified
        // The escaped version should be safe to put in Markup() without throwing
        var markup = new Markup(escaped);
        Assert.NotNull(markup);
    }

    [Fact]
    public void CreateMultipleLogEntries_AllCanBeBuiltInSequence()
    {
        // Arrange
        var queue = new Queue<string>();
        var timestamps = new DateTime[]
        {
            new(2026, 2, 17, 12, 34, 56),
            new(2026, 2, 17, 12, 34, 57),
            new(2026, 2, 17, 12, 34, 58),
        };
        var keys = new[] 
        { 
            MarkupConstants.KeyPressedD,
            MarkupConstants.KeyPressedR,
            MarkupConstants.KeyPressedM,
        };

        // Act
        for (int i = 0; i < timestamps.Length; i++)
        {
            string entry = DebugDisplayFactory.CreateKeyboardLogEntry(keys[i], timestamps[i]);
            queue.Enqueue(entry);
        }

        // Assert
        Assert.Equal(3, queue.Count);
        var list = queue.ToList();
        Assert.Contains("12:34:56", list[0]);
        Assert.Contains("12:34:57", list[1]);
        Assert.Contains("12:34:58", list[2]);
    }

    [Fact]
    public void CreateRadioDataQueueFullSize_AllEntriesEscapeSuccessfully()
    {
        // Arrange
        var radioQueue = new Queue<string>();
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);

        // Act - Fill queue like Program.cs does (max 30 entries)
        for (int i = 0; i < 30; i++)
        {
            var ts = timestamp.AddSeconds(i);
            string data = $"GSI,<XML>,<?xml version=\"1.0\"?><ScannerInfo Mode=\"Mode{i}\"><Freq>462{i:D4}</Freq></ScannerInfo>";
            string entry = DebugDisplayFactory.CreateRadioDataEntry(data, ts);
            radioQueue.Enqueue(entry);
        }

        // Escape and display all
        foreach (var entry in radioQueue)
        {
            string escaped = DebugDisplayFactory.EscapeForDisplay(entry);
            var markup = new Markup(escaped);
            Assert.NotNull(markup);
        }

        // Assert
        Assert.Equal(30, radioQueue.Count);
    }

    [Fact]
    public void DebugTableDisplay_BuildWithFactoryStrings_CompletesWithoutException()
    {
        // Arrange
        var keyboardInputLog = new Queue<string>();
        var rawRadioData = new Queue<string>();
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);

        // Build queues using factory methods (same as Program.cs)
        keyboardInputLog.Enqueue(DebugDisplayFactory.CreateKeyboardLogEntry(MarkupConstants.KeyPressedD, timestamp));
        keyboardInputLog.Enqueue(DebugDisplayFactory.CreateKeyboardLogEntry(MarkupConstants.KeyPressedR, timestamp.AddSeconds(1)));
        
        rawRadioData.Enqueue(DebugDisplayFactory.CreateRadioDataEntry("GSI,<XML>,...", timestamp));
        rawRadioData.Enqueue(DebugDisplayFactory.CreateRadioDataEntry("timeout", timestamp.AddSeconds(1)));

        // Act - Build the debug table exactly as Program.cs does (Render() method)
        var debugTable = new Table().NoBorder().HideHeaders().AddColumns("Keyboard Input", "Raw Radio Traffic");
        var keyboardList = keyboardInputLog.ToList();
        var radioList = rawRadioData.ToList();
        
        int maxRows = Math.Max(keyboardList.Count, radioList.Count);
        for (int i = 0; i < maxRows; i++)
        {
            string keyInput = i < keyboardList.Count 
                ? DebugDisplayFactory.EscapeForDisplay(keyboardList[i])
                : "";
            string radioData = i < radioList.Count 
                ? DebugDisplayFactory.EscapeForDisplay(radioList[i])
                : "";
                
            debugTable.AddRow(
                new Markup(keyInput),
                new Markup(radioData)
            );
        }

        // Assert
        Assert.NotNull(debugTable);
    }

    [Fact]
    public void StatusLineFromFactory_WithValidMarkupTags_CanBePaneledAndDisplayed()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);
        
        // Act - Build the exact string Program.cs renders in the Status panel (line 336)
        string statusLine = DebugDisplayFactory.CreateStatusLine(true, 42, timestamp);
        var markup = new Markup(statusLine);
        var panel = new Panel(markup).Border(BoxBorder.Rounded);

        // Assert
        Assert.NotNull(panel);
        Assert.Contains("CONNECTED", statusLine);
        Assert.Contains("42", statusLine);
    }

    [Fact]
    public void AllKeyConstants_ProduceValidLogEntries()
    {
        // Arrange
        var keys = new[]
        {
            MarkupConstants.KeyPressedD,
            MarkupConstants.KeyPressedR,
            MarkupConstants.KeyPressedM,
            MarkupConstants.KeyPressedV,
            MarkupConstants.KeyPressedSpace,
        };
        var timestamp = new DateTime(2026, 2, 17, 12, 34, 56);

        // Act & Assert
        foreach (var key in keys)
        {
            string entry = DebugDisplayFactory.CreateKeyboardLogEntry(key, timestamp);
            string escaped = DebugDisplayFactory.EscapeForDisplay(entry);
            var markup = new Markup(escaped);
            
            Assert.NotNull(markup);
            Assert.StartsWith("[12:34:56]", entry);
            Assert.Contains("KEY PRESSED:", entry);
        }
    }

    [Fact]
    public void CreateKeyboardLogEntry_WithoutTimestamp_UsesCurrentTime()
    {
        // Arrange
        string keyName = MarkupConstants.KeyPressedD;
        var beforeCall = DateTime.Now;
        
        // Act
        string entry = DebugDisplayFactory.CreateKeyboardLogEntry(keyName);
        var afterCall = DateTime.Now;

        // Assert
        Assert.Contains("KEY PRESSED:", entry);
        Assert.StartsWith("[", entry);
        // Entry should contain a timestamp in the format [HH:mm:ss]
        var timestampPart = entry.Substring(0, 10); // "[HH:mm:ss]"
        Assert.Matches(@"^\[\d{2}:\d{2}:\d{2}\]", entry);
    }

    [Fact]
    public void CreateRadioDataEntry_WithoutTimestamp_UsesCurrentTime()
    {
        // Arrange
        string data = "GSI,<XML>,...";
        
        // Act
        string entry = DebugDisplayFactory.CreateRadioDataEntry(data);

        // Assert
        Assert.Contains("GSI", entry);
        Assert.StartsWith("[", entry);
        Assert.Matches(@"^\[\d{2}:\d{2}:\d{2}\]", entry);
    }

    [Fact]
    public void CreateStatusLine_WithoutTimestamp_UsesCurrentTime()
    {
        // Act
        string statusLine = DebugDisplayFactory.CreateStatusLine(true, 10);

        // Assert
        Assert.Contains("CONNECTED", statusLine);
        Assert.Contains("Radio Data Packets", statusLine);
        Assert.Matches(@"cyan\]\d{2}:\d{2}:\d{2}\[", statusLine);
    }
}


