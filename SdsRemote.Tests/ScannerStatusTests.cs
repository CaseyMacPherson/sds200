namespace SdsRemote.Tests;

using Xunit;
using SdsRemote.Models;

public class ScannerStatusTests
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        // Act
        var status = new ScannerStatus();

        // Assert
        Assert.Equal("---", status.Mode);
        Assert.Equal("---", status.VScreen);
        Assert.Equal("SCANNING", status.SystemName);
        Assert.Equal("...", status.DepartmentName);
        Assert.Equal("...", status.ChannelName);
        Assert.Equal("---", status.Modulation);
        Assert.Equal(0d, status.Frequency);
        Assert.Equal("S0", status.Rssi);
        Assert.Equal(0, status.LastRssiValue);
        Assert.False(status.SignalLocked);
        Assert.Equal("---", status.TgId);
        Assert.Equal("---", status.UnitId);
        Assert.Equal("None", status.LastCommandSent);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        status.SystemName = "Test System";
        status.DepartmentName = "Test Dept";
        status.ChannelName = "Test Channel";
        status.Frequency = 154.5d;
        status.Modulation = "FM";
        status.TgId = "1234";
        status.Rssi = "S5";
        status.LastRssiValue = 5;
        status.SignalLocked = true;

        // Assert
        Assert.Equal("Test System", status.SystemName);
        Assert.Equal("Test Dept", status.DepartmentName);
        Assert.Equal("Test Channel", status.ChannelName);
        Assert.Equal(154.5d, status.Frequency);
        Assert.Equal("FM", status.Modulation);
        Assert.Equal("1234", status.TgId);
        Assert.Equal("S5", status.Rssi);
        Assert.Equal(5, status.LastRssiValue);
        Assert.True(status.SignalLocked);
    }

    [Fact]
    public void TrunkingProperties_Initialized()
    {
        // Arrange & Act
        var status = new ScannerStatus();

        // Assert
        Assert.Equal("---", status.TgId);
        Assert.Equal("---", status.UnitId);
        Assert.Equal("---", status.ServiceType);
        Assert.Equal("---", status.SiteName);
    }

    [Fact]
    public void SearchProperties_Initialized()
    {
        // Arrange & Act
        var status = new ScannerStatus();

        // Assert
        Assert.Equal("---", status.SearchRangeLower);
        Assert.Equal("---", status.SearchRangeUpper);
        Assert.Equal(0, status.HitCount);
    }

    [Fact]
    public void PropertyAttributes_Initialized()
    {
        // Arrange & Act
        var status = new ScannerStatus();

        // Assert
        Assert.Equal("Unmute", status.Mute);
        Assert.Equal("Off", status.Attenuator);
        Assert.Equal("Off", status.AlertLed);
        Assert.Equal("Off", status.Hold);
        Assert.Equal("Off", status.Recording);
        Assert.Equal("---", status.P25Status);
    }

    [Fact]
    public void SignalLockTracking_WorksCorrectly()
    {
        // Arrange
        var status = new ScannerStatus();
        var beforeLock = DateTime.UtcNow;

        // Act - Simulate lock
        status.SignalLocked = true;
        status.LastLockChangeTime = DateTime.UtcNow;
        var afterLock = DateTime.UtcNow;

        // Assert
        Assert.True(status.SignalLocked);
        Assert.True(status.LastLockChangeTime >= beforeLock);
        Assert.True(status.LastLockChangeTime <= afterLock);
    }

    [Fact]
    public void MultipleInstances_Independent()
    {
        // Arrange
        var status1 = new ScannerStatus();
        var status2 = new ScannerStatus();

        // Act
        status1.SystemName = "System 1";
        status2.SystemName = "System 2";

        // Assert
        Assert.Equal("System 1", status1.SystemName);
        Assert.Equal("System 2", status2.SystemName);
    }

    [Theory]
    [InlineData("FM")]
    [InlineData("P25")]
    [InlineData("DMR")]
    public void Modulation_StoresVariousTypes(string modType)
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        status.Modulation = modType;

        // Assert
        Assert.Equal(modType, status.Modulation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(29)]
    public void Volume_StoresValidRange(int vol)
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        status.Volume = vol;

        // Assert
        Assert.Equal(vol, status.Volume);
    }

    [Fact]
    public void LastCommandSent_TracksMessages()
    {
        // Arrange
        var status = new ScannerStatus();

        // Act
        status.LastCommandSent = "✅ Status Updated";

        // Assert
        Assert.Equal("✅ Status Updated", status.LastCommandSent);
    }
}
