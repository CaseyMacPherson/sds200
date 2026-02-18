namespace SdsRemote.Tests;

using Xunit;
using SdsRemote.Models;
using SdsRemote.Logic;

public class UnidenParserTests
{
    [Fact]
    public void UpdateStatus_ConventionalScan_ParsesCorrectly()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.ConventionalScanXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("conventional_scan", status.VScreen);
        Assert.Equal("FDNY", status.SystemName);
        Assert.Equal("Fire", status.DepartmentName);
        Assert.Equal("Dispatch", status.ChannelName);
        Assert.Equal("FM", status.Modulation);
        Assert.Equal(154.2800d, status.Frequency);
        Assert.Equal("S3", status.Rssi);
        Assert.Equal(3, status.LastRssiValue);
        Assert.Equal(15, status.Volume);
        Assert.Equal(10, status.Squelch);
    }

    [Fact]
    public void UpdateStatus_TrunkScan_ParsesTgidAndSite()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.TrunkScanXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("trunk_scan", status.VScreen);
        Assert.Equal("Police RAN", status.SystemName);
        Assert.Equal("Central", status.SiteName);
        Assert.Equal("1234", status.TgId);
        Assert.Equal("5678", status.UnitId);
        Assert.Equal("Traffic", status.ChannelName);
        Assert.Equal(863.5625d, status.Frequency);
        Assert.Equal("S5", status.Rssi);
    }

    [Fact]
    public void UpdateStatus_ToneOut_ParsesTones()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.ToneOutXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("tone_out", status.VScreen);
        Assert.Equal("Alert", status.ChannelName);
        Assert.Equal("100.0", status.ToneA);
        Assert.Equal("110.0", status.ToneB);
        Assert.Equal(146.5200d, status.Frequency);
        Assert.Equal("FM", status.Modulation);
    }

    [Fact]
    public void UpdateStatus_QuickSearch_ParsesSearchRange()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.QuickSearchXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("quick_search", status.VScreen);
        Assert.Equal("150.0000MHz", status.SearchRangeLower);
        Assert.Equal("160.0000MHz", status.SearchRangeUpper);
        Assert.Equal("FM", status.Modulation);
        Assert.Equal(155.1600d, status.Frequency);
    }

    [Fact]
    public void UpdateStatus_DiscoveryConventional_ParsesHitCount()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.DiscoveryConventionalXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("discovery_conventional", status.VScreen);
        Assert.Equal(5, status.HitCount);
        Assert.Equal(151.8950d, status.Frequency);
    }

    [Fact]
    public void UpdateStatus_DiscoveryTrunking_ParsesSystemAndHits()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.DiscoveryTrunkingXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("discovery_trunking", status.VScreen);
        Assert.Equal("Rail System", status.SystemName);
        Assert.Equal("Dispatch", status.SiteName);
        Assert.Equal("5001", status.TgId);
        Assert.Equal("Dispatchers", status.ChannelName);
        Assert.Equal(12, status.HitCount);
    }

    [Fact]
    public void UpdateStatus_MalformedTgid_DisplaysDefault()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.TrunkScanMalformedTgidXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("trunk_scan", status.VScreen);
        // The parser reads "TGID" as the attribute value, but display code filters it
        Assert.Equal("TGID", status.TgId);
    }

    [Fact]
    public void UpdateStatus_NoSignal_RssiIsZero()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.NoSignalXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("S0", status.Rssi);
        Assert.Equal(0, status.LastRssiValue);
    }

    [Fact]
    public void UpdateStatus_WeatherMode_ParsesCorrectly()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.WeatherXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("wx_alert", status.VScreen);
        Assert.Equal("WX1", status.ChannelName);
        Assert.Equal(162.5500d, status.Frequency);
    }

    [Fact]
    public void UpdateStatus_InvalidXml_ReturnsFalse()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.InvalidXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateStatus_MinimalXml_HandlesGracefully()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = GsiTestData.MinimalXml;

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.True(result);
        Assert.Equal("unknown", status.VScreen);
        Assert.Equal("S1", status.Rssi);
        Assert.Equal(1, status.LastRssiValue);
    }

    [Fact]
    public void UpdateStatus_EmptyString_ReturnsFalse()
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        bool result = UnidenParser.UpdateStatus(status, "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateStatus_NullString_ReturnsFalse()
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        bool result = UnidenParser.UpdateStatus(status, null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateStatus_MissingClosingTag_ReturnsFalse()
    {
        // Arrange
        var status = new ScannerStatus();
        var xml = "GSI,<XML>,<?xml version=\"1.0\"?><ScannerInfo Mode=\"Test\">";

        // Act
        bool result = UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateStatus_MultipleVScreens_PicksCorrectParser()
    {
        // Test that different V_Screen values trigger different parsing paths
        var modes = new[] 
        { 
            ("conventional_scan", GsiTestData.ConventionalScanXml),
            ("trunk_scan", GsiTestData.TrunkScanXml),
            ("tone_out", GsiTestData.ToneOutXml),
            ("quick_search", GsiTestData.QuickSearchXml),
            ("discovery_conventional", GsiTestData.DiscoveryConventionalXml),
            ("discovery_trunking", GsiTestData.DiscoveryTrunkingXml),
            ("wx_alert", GsiTestData.WeatherXml),
        };

        foreach (var (expectedMode, xml) in modes)
        {
            var status = new ScannerStatus();
            bool result = UnidenParser.UpdateStatus(status, xml);
            
            Assert.True(result, $"Failed to parse {expectedMode}");
            Assert.Equal(expectedMode, status.VScreen);
        }
    }

    [Fact]
    public void UpdateStatus_PreservesDefaultsForMissingElements()
    {
        // Arrange
        var status = new ScannerStatus();
        string xml = """
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo Mode="Test" V_Screen="conventional_scan">
  <Property Rssi="2" />
</ScannerInfo>
""";

        // Act
        UnidenParser.UpdateStatus(status, xml);

        // Assert
        Assert.Equal("SCANNING", status.SystemName); // No System element = default
        Assert.Equal("...", status.DepartmentName); // No Department element
        Assert.Equal("...", status.ChannelName); // No ConvFrequency element
        Assert.Equal("S2", status.Rssi); // Property was parsed
        Assert.Equal(0d, status.Frequency); // No frequency = 0.0
    }

    [Fact]
    public void UpdateStatus_ExtractsFrequencyCorrectly()
    {
        // Test various frequency formats
        var testCases = new[]
        {
            ("154.2800MHz", 154.2800d),
            ("863.5625MHz", 863.5625d),
            ("162.5500MHz", 162.5500d),
            ("146.5200MHz", 146.5200d),
        };

        foreach (var (freqStr, expected) in testCases)
        {
            var status = new ScannerStatus();
            string xml = $"""
GSI,<XML>,<?xml version="1.0"?>
<ScannerInfo>
  <ConvFrequency Freq="{freqStr}" />
  <Property Rssi="1" />
</ScannerInfo>
""";
            
            UnidenParser.UpdateStatus(status, xml);
            Assert.Equal(expected, status.Frequency, precision: 4);
        }
    }
}
