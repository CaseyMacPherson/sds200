namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// No-op logger implementation used as a safe default (Null Object Pattern).
/// Inject this when logging is optional to avoid null-check boilerplate.
/// </summary>
public sealed class NullLogger : ILogger
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly NullLogger Instance = new();

    /// <inheritdoc/>
    public void LogDebug(string message) { }

    /// <inheritdoc/>
    public void LogInfo(string message) { }

    /// <inheritdoc/>
    public void LogWarning(string message) { }

    /// <inheritdoc/>
    public void LogError(string message, Exception? ex = null) { }
}

