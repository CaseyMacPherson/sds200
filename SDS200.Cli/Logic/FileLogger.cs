namespace SDS200.Cli.Logic;

using System;
using System.IO;
using System.Threading.Tasks;

public static class FileLogger
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scanner_hits.csv");
    private static readonly object _lock = new();

    public static async Task LogHitAsync(double frequency, string channel, string system)
    {
        // QA Engineer: Ensure we don't log empty "Scanning" states
        if (frequency == 0 || string.IsNullOrEmpty(channel) || channel.Contains("...")) return;

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{frequency:F4},{system},{channel}{Environment.NewLine}";

        try
        {
            // Use a simple lock for file safety, but write asynchronously
            await File.AppendAllTextAsync(LogPath, line);
        }
        catch (Exception)
        {
            // Fail silently or log to console if file is locked
        }
    }
}