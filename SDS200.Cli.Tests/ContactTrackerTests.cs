namespace SdsRemote.Tests;

using Xunit;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Logic;
using SDS200.Cli.Tests;

public class ContactTrackerTests
{
    private static (ContactTracker tracker, Queue<ContactLogEntry> log, FakeTimeProvider time)
        CreateTracker(int maxSize = 10)
    {
        var log = new Queue<ContactLogEntry>();
        var time = new FakeTimeProvider();
        var tracker = new ContactTracker(log, time, maxSize);
        return (tracker, log, time);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalAppearsOnNoSignal_CreatesContactEntry()
    {
        // Arrange
        var (tracker, log, _) = CreateTracker();
        var status = new ScannerStatusBuilder()
            .WithRssi(3)
            .WithSystem("FDNY")
            .WithChannel("Dispatch")
            .Build();

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.Single(log);
        Assert.Equal("FDNY", log.Peek().SystemName);
        Assert.Equal("Dispatch", log.Peek().ChannelName);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalAppearsOnNoSignal_SetsSignalLockedTrue()
    {
        // Arrange
        var (tracker, _, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(5).Build();

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.True(status.SignalLocked);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalAlreadyLocked_DoesNotCreateDuplicateEntry()
    {
        // Arrange
        var (tracker, log, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(5).WithSignalLocked(true).Build();

        // Act — call twice with signal still present
        tracker.ProcessSignalUpdate(status);
        tracker.ProcessSignalUpdate(status);

        // Assert — only one entry (already locked on first call)
        Assert.Empty(log);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalDrops_SetsSignalLockedFalse()
    {
        // Arrange
        var (tracker, _, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(0).WithSignalLocked(true).Build();

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.False(status.SignalLocked);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalDrops_DoesNotCreateContactEntry()
    {
        // Arrange
        var (tracker, log, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(0).WithSignalLocked(true).Build();

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.Empty(log);
    }

    [Fact]
    public void ProcessSignalUpdate_SignalLockCycle_RecordsEachNewLock()
    {
        // Arrange
        var (tracker, log, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(0).Build();

        // Act — two complete lock/unlock cycles
        status.LastRssiValue = 3; tracker.ProcessSignalUpdate(status); // lock
        status.LastRssiValue = 0; tracker.ProcessSignalUpdate(status); // unlock
        status.LastRssiValue = 5; tracker.ProcessSignalUpdate(status); // lock again
        status.LastRssiValue = 0; tracker.ProcessSignalUpdate(status); // unlock

        // Assert
        Assert.Equal(2, log.Count);
    }

    [Fact]
    public void ProcessSignalUpdate_ExceedsMaxSize_EvictsOldestEntry()
    {
        // Arrange
        const int maxSize = 3;
        var (tracker, log, _) = CreateTracker(maxSize);

        // Act — produce maxSize+1 contacts via lock/unlock cycles
        for (int i = 0; i < maxSize + 1; i++)
        {
            var status = new ScannerStatusBuilder()
                .WithRssi(3)
                .WithChannel($"CH{i}")
                .Build();
            tracker.ProcessSignalUpdate(status); // lock

            var unlock = new ScannerStatusBuilder().WithRssi(0).WithSignalLocked(true).Build();
            tracker.ProcessSignalUpdate(unlock); // unlock
        }

        // Assert
        Assert.Equal(maxSize, log.Count);
    }

    [Fact]
    public void GetContacts_ReturnsAllEntries()
    {
        // Arrange
        var (tracker, _, _) = CreateTracker();
        var s1 = new ScannerStatusBuilder().WithRssi(3).WithChannel("CH1").Build();
        var s2u = new ScannerStatusBuilder().WithRssi(0).WithSignalLocked(true).Build();
        var s2 = new ScannerStatusBuilder().WithRssi(5).WithChannel("CH2").Build();

        tracker.ProcessSignalUpdate(s1);
        tracker.ProcessSignalUpdate(s2u);
        tracker.ProcessSignalUpdate(s2);

        // Act
        var contacts = tracker.GetContacts().ToList();

        // Assert
        Assert.Equal(2, contacts.Count);
        Assert.Equal("CH1", contacts[0].ChannelName);
        Assert.Equal("CH2", contacts[1].ChannelName);
    }

    [Fact]
    public void Count_ReflectsNumberOfContacts()
    {
        // Arrange
        var (tracker, _, _) = CreateTracker();
        var status = new ScannerStatusBuilder().WithRssi(3).Build();

        // Act
        tracker.ProcessSignalUpdate(status);

        // Assert
        Assert.Equal(1, tracker.Count);
    }
}

