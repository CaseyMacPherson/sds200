using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Logic;

/// <summary>
/// Logs scanner contact hits to a CSV file.
/// Injectable and testable via <see cref="IFileSystem"/> and <see cref="ITimeProvider"/>.
/// </summary>
public class FileLogger
{
    private readonly IFileSystem _fileSystem;
    private readonly ITimeProvider _timeProvider;
    private readonly string _logPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// Creates a <see cref="FileLogger"/> writing to the default path beside the executable.
    /// </summary>
    public FileLogger() : this(SystemFileSystem.Instance, new SystemTimeProvider()) { }

    /// <summary>
    /// Creates a <see cref="FileLogger"/> with injected dependencies.
    /// </summary>
    /// <param name="fileSystem">File system abstraction used for all I/O operations.</param>
    /// <param name="timeProvider">Time provider used to stamp each log entry.</param>
    /// <param name="logPath">
    /// Optional override for the log file path.
    /// Defaults to <c>scanner_hits.csv</c> beside the application executable.
    /// </param>
    public FileLogger(IFileSystem fileSystem, ITimeProvider timeProvider, string? logPath = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logPath = logPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scanner_hits.csv");
    }

    /// <summary>
    /// Appends a contact hit to the CSV log.
    /// Skips entries where the frequency is zero or the channel name is empty/default.
    /// </summary>
    /// <param name="frequency">Frequency in MHz.</param>
    /// <param name="channel">Channel or TGID name.</param>
    /// <param name="system">System name.</param>
    public async Task LogHitAsync(double frequency, string channel, string system)
    {
        // Skip "scanning" idle states — no meaningful contact to log
        if (frequency == 0 || string.IsNullOrEmpty(channel) || channel.Contains("..."))
            return;

        string line = $"{_timeProvider.Now:yyyy-MM-dd HH:mm:ss},{frequency:F4},{system},{channel}{Environment.NewLine}";

        // Use a semaphore rather than a lock so the wait is async-safe
        await _writeLock.WaitAsync();
        try
        {
            await _fileSystem.AppendAllTextAsync(_logPath, line);
        }
        catch (IOException)
        {
            // Transient file-lock or disk error — skip this entry rather than crashing
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>Gets the full path of the log file being written.</summary>
    public string LogPath => _logPath;
}