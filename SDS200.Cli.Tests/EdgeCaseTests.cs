namespace SdsRemote.Tests;

using Xunit;
using SDS200.Cli.Models;
using SDS200.Cli.Logic;

public class EdgeCaseTests
{
    [Fact]
    public void Parser_HandlesVeryHighFrequency()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <ConvFrequency Freq="6000.0000MHz" />
  <Property Rssi="3" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal(6000.0000d, status.Frequency, precision: 4);
    }

    [Fact]
    public void Parser_HandlesVeryLowFrequency()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <ConvFrequency Freq="25.0000MHz" />
  <Property Rssi="3" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal(25.0000d, status.Frequency, precision: 4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    public void Parser_HandlesAllRssiValues(int rssiValue)
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = $"""
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <Property Rssi="{rssiValue}" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal($"S{rssiValue}", status.Rssi);
        Assert.Equal(rssiValue, status.LastRssiValue);
    }

    [Fact]
    public void Parser_HandlesEmptyAttributes()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="" V_Screen="">
  <System Name="" />
  <ConvFrequency Freq="" Mod="" />
  <Property Rssi="" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("SCANNING", status.SystemName);
        Assert.Equal("---", status.Modulation);
    }

    [Fact]
    public void Parser_HandlesSelfClosingElements()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="Test" />
  <Property Rssi="2" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("Test", status.SystemName);
    }

    [Fact]
    public void Parser_HandlesSpecialCharactersInNames()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="Test &amp; More" />
  <ConvFrequency Name="Channel #1" />
  <Property Rssi="3" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("Test & More", status.SystemName);
        Assert.Equal("Channel #1", status.ChannelName);
    }

    [Fact]
    public void Parser_HandlesWhitespaceInValues()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="  System Name  " />
  <ConvFrequency Name="  Channel 1  " />
  <Property Rssi="2" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        // XML parser typically preserves whitespace within attributes
        Assert.Contains("System", status.SystemName);
    }

    [Fact]
    public void Parser_HandlesLargeNumberOfElements()
    {
        // Create a large response with many of the same elements
        // (though the parser only reads one)
        var status = new ScannerStatus();
        string xml = $"""
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="MainSystem" />
  {string.Concat(Enumerable.Range(1, 100).Select(i => $"<Extra Index=\"{i}\" />\n"))}
  <ConvFrequency Name="MainChannel" Freq="154.2800MHz" />
  <Property Rssi="3" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("MainSystem", status.SystemName);
        Assert.Equal("MainChannel", status.ChannelName);
    }

    [Fact]
    public void Parser_HandlesUnicodeCharacters()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="Système Police" />
  <ConvFrequency Name="Canal №1" />
  <Property Rssi="2" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Contains("Syst", status.SystemName);
        Assert.Contains("Canal", status.ChannelName);
    }

    [Fact]
    public void Parser_HandlesEnvelopeMissingGsi()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="NoEnvelope" />
  <Property Rssi="2" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("NoEnvelope", status.SystemName);
    }

    [Fact]
    public void Parser_HandlesMultilineResponse()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = "GSI,<XML>," + GsiTestData.ConventionalScanXml.Split("GSI,<XML>,")[1];

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContactLogEntry_HandlesLongDurations()
    {
        // Arrange
        var entry = new ContactLogEntry
        {
            LockTime = DateTime.UtcNow.AddHours(-1) // 1 hour ago
        };

        // Act
        var duration = entry.DurationSeconds;

        // Assert
        Assert.True(duration >= 3599); // At least 59:59
    }

    [Fact]
    public void ScannerStatus_HandlesPropertyChaining()
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        status.SystemName = "Sys";
        string retrieved = status.SystemName;

        // Assert
        Assert.Equal("Sys", retrieved);
    }

    [Fact]
    public void Parser_HandlesDuplicateElements()
    {
        // When XML has duplicate elements, XElement.Element() returns the first
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <System Name="First" />
  <System Name="Second" />
  <ConvFrequency Name="Ch" Freq="154.0000MHz" />
  <Property Rssi="3" />
</ScannerInfo>
""";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("First", status.SystemName); // First element read
    }

    [Fact]
    public void Parser_RssiFormattingConsistency()
    {
        // Ensure that numeric RSSI is always extracted correctly
        var testValues = new[] { "0", "1", "5", "9" };
        
        foreach (var rssiVal in testValues)
        {
            var status = new ScannerStatus();
            string xml = $"""
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo V_Screen="test">
  <Property Rssi="{rssiVal}" />
</ScannerInfo>
""";
            
            UnidenParser.UpdateStatus(status, xml);
            
            Assert.Equal($"S{rssiVal}", status.Rssi);
            Assert.Equal(int.Parse(rssiVal), status.LastRssiValue);
        }
    }
}
