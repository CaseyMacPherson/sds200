namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// Abstraction for time-related operations.
/// Allows injecting fake time for testing.
/// </summary>
public interface ITimeProvider
{
    /// <summary>Gets the current UTC time.</summary>
    DateTime UtcNow { get; }

    /// <summary>Gets the current local time.</summary>
    DateTime Now { get; }
}

/// <summary>
/// Production implementation using system clock.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <summary>Gets the current UTC time from the system clock.</summary>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Gets the current local time from the system clock.</summary>
    public DateTime Now => DateTime.Now;
}

