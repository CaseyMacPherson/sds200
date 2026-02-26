using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Logic;

/// <summary>
/// Production <see cref="IFileSystem"/> implementation that delegates to
/// <see cref="System.IO.File"/> and <see cref="System.IO.Path"/>.
/// </summary>
public sealed class SystemFileSystem : IFileSystem
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly SystemFileSystem Instance = new();

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path)
        => File.ReadAllTextAsync(path);

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string contents)
        => File.WriteAllTextAsync(path, contents);

    /// <inheritdoc/>
    public Task AppendAllTextAsync(string path, string contents)
        => File.AppendAllTextAsync(path, contents);

    /// <inheritdoc/>
    public bool FileExists(string path)
        => File.Exists(path);

    /// <inheritdoc/>
    public void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

