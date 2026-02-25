using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Tests;

/// <summary>
/// Fake time provider for testing.
/// Allows controlling time in unit tests.
/// </summary>
public class FakeTimeProvider : ITimeProvider
{
    private DateTime _utcNow;
    private TimeSpan _localOffset;

    /// <summary>
    /// Creates a FakeTimeProvider with the specified starting time.
    /// </summary>
    /// <param name="utcNow">The initial UTC time.</param>
    /// <param name="localOffset">Offset from UTC for local time (default: 0).</param>
    public FakeTimeProvider(DateTime? utcNow = null, TimeSpan? localOffset = null)
    {
        _utcNow = utcNow ?? new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc);
        _localOffset = localOffset ?? TimeSpan.Zero;
    }

    /// <summary>Gets the current fake UTC time.</summary>
    public DateTime UtcNow => _utcNow;

    /// <summary>Gets the current fake local time (UTC + offset).</summary>
    public DateTime Now => _utcNow + _localOffset;

    /// <summary>
    /// Sets the current time to a specific value.
    /// </summary>
    /// <param name="utcNow">The new UTC time.</param>
    public void SetTime(DateTime utcNow)
    {
        _utcNow = utcNow;
    }

    /// <summary>
    /// Advances time by the specified duration.
    /// </summary>
    /// <param name="duration">How much to advance time.</param>
    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }
}

