namespace SDS200.Cli.Abstractions.Core;

using SDS200.Cli.Abstractions.Models;

/// <summary>
/// Abstraction for parsing raw scanner responses into <see cref="ScannerStatus"/>.
/// Implementations handle specific response formats (e.g., GSI XML, MDL text).
/// </summary>
public interface IResponseParser
{
    /// <summary>
    /// Attempts to parse <paramref name="rawData"/> and update <paramref name="status"/>.
    /// </summary>
    /// <param name="status">The status object to update in-place.</param>
    /// <param name="rawData">The raw response string received from the scanner.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> otherwise.</returns>
    bool UpdateStatus(ScannerStatus status, string rawData);
}

