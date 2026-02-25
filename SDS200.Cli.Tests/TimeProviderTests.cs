using Xunit;
using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Logic;

namespace SDS200.Cli.Tests;

/// <summary>
/// Tests demonstrating the ITimeProvider abstraction benefits.
/// </summary>
public class TimeProviderTests
{
    [Fact]
    public void FakeTimeProvider_UtcNow_ReturnsConfiguredTime()
    {
        // Arrange
        var expectedTime = new DateTime(2026, 2, 25, 14, 30, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(expectedTime);

        // Act
        var actualTime = timeProvider.UtcNow;

        // Assert
        Assert.Equal(expectedTime, actualTime);
    }

    [Fact]
    public void FakeTimeProvider_Advance_MovesTimeForward()
    {
        // Arrange
        var startTime = new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(startTime);

        // Act
        timeProvider.Advance(TimeSpan.FromMinutes(30));

        // Assert
        Assert.Equal(startTime.AddMinutes(30), timeProvider.UtcNow);
    }

    [Fact]
    public void FakeTimeProvider_SetTime_ChangesCurrentTime()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var newTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);

        // Act
        timeProvider.SetTime(newTime);

        // Assert
        Assert.Equal(newTime, timeProvider.UtcNow);
    }

    [Fact]
    public void FakeTimeProvider_Now_RespectsLocalOffset()
    {
        // Arrange
        var utcTime = new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc);
        var localOffset = TimeSpan.FromHours(-5); // EST
        var timeProvider = new FakeTimeProvider(utcTime, localOffset);

        // Act
        var localTime = timeProvider.Now;

        // Assert
        Assert.Equal(utcTime.AddHours(-5), localTime);
    }

    [Fact]
    public void SystemTimeProvider_UtcNow_ReturnsCurrentTime()
    {
        // Arrange
        var timeProvider = new SystemTimeProvider();
        var before = DateTime.UtcNow;

        // Act
        var actual = timeProvider.UtcNow;
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(actual, before, after);
    }

    [Fact]
    public void ContactTracker_UsesInjectedTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTime(2026, 2, 25, 15, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var contactLog = new Queue<ContactLogEntry>();
        var tracker = new ContactTracker(contactLog, timeProvider);
        var status = new ScannerStatus 
        { 
            LastRssiValue = 5,  // Signal present
            SignalLocked = false 
        };

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.True(status.SignalLocked);
        Assert.Equal(fixedTime, status.LastLockChangeTime);
    }

    [Fact]
    public void ContactTracker_DefaultsToSystemTimeProvider()
    {
        // Arrange - no time provider specified
        var contactLog = new Queue<ContactLogEntry>();
        var tracker = new ContactTracker(contactLog);
        var status = new ScannerStatus 
        { 
            LastRssiValue = 5,
            SignalLocked = false 
        };
        var before = DateTime.UtcNow;

        // Act
        tracker.ProcessSignalUpdate(status);
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(status.SignalLocked);
        Assert.InRange(status.LastLockChangeTime, before, after);
    }
}

