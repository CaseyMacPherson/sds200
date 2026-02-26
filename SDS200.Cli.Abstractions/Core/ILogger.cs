namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// Abstraction for application-level logging.
/// Decouples business logic from concrete logging implementations.
/// </summary>
public interface ILogger
{
    /// <summary>Logs a debug-level message.</summary>
    /// <param name="message">The message to log.</param>
    void LogDebug(string message);

    /// <summary>Logs an informational message.</summary>
    /// <param name="message">The message to log.</param>
    void LogInfo(string message);

    /// <summary>Logs a warning message.</summary>
    /// <param name="message">The message to log.</param>
    void LogWarning(string message);

    /// <summary>Logs an error message with an optional exception.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception associated with the error.</param>
    void LogError(string message, Exception? ex = null);
}

