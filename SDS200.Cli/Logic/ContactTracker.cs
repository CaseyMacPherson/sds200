using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Abstractions.Models;

namespace SDS200.Cli.Logic;

/// <summary>
/// Tracks signal lock state and manages contact log entries.
/// Extracts contact tracking logic from the main polling loop.
/// </summary>
public class ContactTracker
{
    private readonly Queue<ContactLogEntry> _contactLog;
    private readonly int _maxContactLogSize;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Creates a new ContactTracker.
    /// </summary>
    /// <param name="contactLog">Queue to store contact log entries.</param>
    /// <param name="timeProvider">Time provider for timestamps (optional, defaults to system time).</param>
    /// <param name="maxContactLogSize">Maximum entries to keep (default 30).</param>
    public ContactTracker(
        Queue<ContactLogEntry> contactLog,
        ITimeProvider? timeProvider = null,
        int maxContactLogSize = 30)
    {
        _contactLog = contactLog ?? throw new ArgumentNullException(nameof(contactLog));
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _maxContactLogSize = maxContactLogSize;
    }

    /// <summary>
    /// Processes a signal update and tracks contact state.
    /// Creates a new contact entry when signal locks, updates duration when signal drops.
    /// </summary>
    /// <param name="status">Current scanner status with RSSI and signal state.</param>
    public void ProcessSignalUpdate(ScannerStatus status)
    {
        // Track contact based on RSSI threshold (> 0 = signal detected)
        bool signalPresent = status.LastRssiValue > 0;

        if (signalPresent && !status.SignalLocked)
        {
            // Signal just locked on
            status.SignalLocked = true;
            status.LastLockChangeTime = _timeProvider.UtcNow;
            
            var entry = ContactLogEntry.FromStatus(status);
            EnqueueCapped(entry);
        }
        else if (!signalPresent && status.SignalLocked)
        {
            // Signal just dropped
            status.SignalLocked = false;
            status.LastLockChangeTime = _timeProvider.UtcNow;
        }
    }

    /// <summary>
    /// Gets the current contact log as an enumerable (for rendering).
    /// </summary>
    public IEnumerable<ContactLogEntry> GetContacts() => _contactLog;

    /// <summary>
    /// Gets the count of contacts in the log.
    /// </summary>
    public int Count => _contactLog.Count;

    private void EnqueueCapped(ContactLogEntry entry)
    {
        _contactLog.Enqueue(entry);
        while (_contactLog.Count > _maxContactLogSize) _contactLog.Dequeue();
    }
}

