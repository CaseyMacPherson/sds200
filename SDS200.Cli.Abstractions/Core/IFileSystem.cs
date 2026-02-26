namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// Abstraction for file system operations.
/// Enables testing without actual file I/O by injecting fake implementations.
/// </summary>
public interface IFileSystem
{
    /// <summary>Reads all text from a file asynchronously.</summary>
    /// <param name="path">The full path to the file.</param>
    Task<string> ReadAllTextAsync(string path);

    /// <summary>Writes all text to a file asynchronously, overwriting if it exists.</summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="contents">The text to write.</param>
    Task WriteAllTextAsync(string path, string contents);

    /// <summary>Appends text to a file asynchronously, creating it if it does not exist.</summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="contents">The text to append.</param>
    Task AppendAllTextAsync(string path, string contents);

    /// <summary>Returns <c>true</c> if the specified file exists.</summary>
    /// <param name="path">The full path to the file.</param>
    bool FileExists(string path);

    /// <summary>Deletes the specified file if it exists.</summary>
    /// <param name="path">The full path to the file.</param>
    void DeleteFile(string path);
}

