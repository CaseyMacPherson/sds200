using SDS200.Cli.Abstractions.Core;

namespace SdsRemote.Tests;

/// <summary>
/// In-memory <see cref="IFileSystem"/> for use in unit tests.
/// Stores file contents in a dictionary — no disk I/O performed.
/// </summary>
public class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Snapshot of all files written (path → last written content).</summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    /// <summary>Accumulates every append call in order (path → list of appended strings).</summary>
    public Dictionary<string, List<string>> AppendLog { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path)
    {
        if (!_files.TryGetValue(path, out var content))
            throw new FileNotFoundException($"InMemoryFileSystem: file not found: {path}");
        return Task.FromResult(content);
    }

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string contents)
    {
        _files[path] = contents;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AppendAllTextAsync(string path, string contents)
    {
        if (!_files.ContainsKey(path)) _files[path] = "";
        _files[path] += contents;

        if (!AppendLog.ContainsKey(path)) AppendLog[path] = new List<string>();
        AppendLog[path].Add(contents);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public bool FileExists(string path) => _files.ContainsKey(path);

    /// <inheritdoc/>
    public void DeleteFile(string path) => _files.Remove(path);
}

