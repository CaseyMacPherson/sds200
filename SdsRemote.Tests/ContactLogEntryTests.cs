namespace SdsRemote.Tests;

using Xunit;
using SdsRemote.Models;
using System.Threading;

public class ContactLogEntryTests
{
    [Fact]
    public void FromStatus_CreatesValidContactLogEntry()
    {
        // Arrange
        var status = new ScannerStatus
        {
            Frequency = 154.2800d,
            Modulation = "FM",
            VScreen = "conventional_scan",
            SystemName = "FDNY",
            ChannelName = "Dispatch",
            TgId = "---",
            SiteName = "---",
            Rssi = "S3"
        };

        // Act
        var entry = ContactLogEntry.FromStatus(status);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal(154.2800d, entry.Frequency);
        Assert.Equal("FM", entry.Modulation);
        Assert.Equal("conventional_scan", entry.Mode);
        Assert.Equal("FDNY", entry.SystemName);
        Assert.Equal("Dispatch", entry.ChannelName);
        Assert.Equal("S3", entry.Rssi);
    }

    [Fact]
    public void FromStatus_CapturesTonkingInfo()
    {
        // Arrange
        var status = new ScannerStatus
        {
            Frequency = 863.5625d,
            Modulation = "P25",
            VScreen = "trunk_scan",
            SystemName = "Police RAN",
            ChannelName = "Traffic",
            TgId = "1234",
            SiteName = "Central",
            Rssi = "S5"
        };

        // Act
        var entry = ContactLogEntry.FromStatus(status);

        // Assert
        Assert.Equal("1234", entry.TgId);
        Assert.Equal("Central", entry.SiteName);
        Assert.Equal("trunk_scan", entry.Mode);
    }

    [Fact]
    public void DurationSeconds_CalculatesCorrectly()
    {
        // Arrange
        var entry = new ContactLogEntry
        {
            LockTime = DateTime.UtcNow.AddSeconds(-5)
        };

        // Act
        double duration = entry.DurationSeconds;

        // Assert
        Assert.True(duration >= 4.5 && duration <= 5.5, $"Expected ~5 seconds, got {duration}");
    }

    [Fact]
    public void DurationSeconds_IncrementsOverTime()
    {
        // Arrange
        var entry = new ContactLogEntry
        {
            LockTime = DateTime.UtcNow.AddSeconds(-2)
        };

        // Act
        var dur1 = entry.DurationSeconds;
        Thread.Sleep(100); // Wait 100ms
        var dur2 = entry.DurationSeconds;

        // Assert
        Assert.True(dur2 > dur1, "Duration should increase over time");
    }

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        // Act
        var entry = new ContactLogEntry();

        // Assert
        Assert.Equal(0d, entry.Frequency);
        Assert.Equal("---", entry.Modulation);
        Assert.Equal("---", entry.Mode);
        Assert.Equal("---", entry.SystemName);
        Assert.Equal("---", entry.ChannelName);
        Assert.Equal("---", entry.TgId);
        Assert.Equal("---", entry.SiteName);
        Assert.Equal("S0", entry.Rssi);
    }

    [Fact]
    public void LockTime_IsUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var entry = ContactLogEntry.FromStatus(new ScannerStatus());
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(entry.LockTime >= before);
        Assert.True(entry.LockTime <= after);
    }

    [Fact]
    public void MultipleEntries_Independent()
    {
        // Arrange
        var status1 = new ScannerStatus { Frequency = 154.2800d, SystemName = "System1" };
        var status2 = new ScannerStatus { Frequency = 863.5625d, SystemName = "System2" };

        // Act
        var entry1 = ContactLogEntry.FromStatus(status1);
        Thread.Sleep(50);
        var entry2 = ContactLogEntry.FromStatus(status2);

        // Assert
        Assert.Equal(154.2800d, entry1.Frequency);
        Assert.Equal(863.5625d, entry2.Frequency);
        Assert.Equal("System1", entry1.SystemName);
        Assert.Equal("System2", entry2.SystemName);
        Assert.True(entry2.LockTime > entry1.LockTime);
    }

    [Fact]
    public void ZeroDurationAtCreation()
    {
        // Arrange & Act
        var entry = new ContactLogEntry { LockTime = DateTime.UtcNow };
        
        // Assert
        Assert.True(entry.DurationSeconds >= 0 && entry.DurationSeconds < 0.1);
    }

    [Theory]
    [InlineData("FM")]
    [InlineData("P25")]
    [InlineData("DMR")]
    public void StoresModulationType(string modType)
    {
        // Arrange
        var entry = new ContactLogEntry { Modulation = modType };

        // Assert
        Assert.Equal(modType, entry.Modulation);
    }

    [Theory]
    [InlineData("conventional_scan")]
    [InlineData("trunk_scan")]
    [InlineData("tone_out")]
    [InlineData("discovery_trunking")]
    public void StoresModeType(string modeType)
    {
        // Arrange
        var entry = new ContactLogEntry { Mode = modeType };

        // Assert
        Assert.Equal(modeType, entry.Mode);
    }

    [Fact]
    public void FrequencyPrecision_PreservesDecimals()
    {
        // Arrange
        const double freq = 154.2875d;
        var entry = new ContactLogEntry { Frequency = freq };

        // Act
        var retrieved = entry.Frequency;

        // Assert
        Assert.Equal(freq, retrieved, precision: 4);
    }
}
