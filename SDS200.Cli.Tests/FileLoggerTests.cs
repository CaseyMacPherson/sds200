namespace SdsRemote.Tests;

using Xunit;
using SDS200.Cli.Logic;
using SDS200.Cli.Tests;

public class FileLoggerTests
{
    private static FileLogger CreateLogger(out InMemoryFileSystem fs, out FakeTimeProvider time, string path = "/test/log.csv")
    {
        fs = new InMemoryFileSystem();
        time = new FakeTimeProvider();
        return new FileLogger(fs, time, path);
    }

    [Fact]
    public async Task LogHitAsync_WritesCorrectCsvLine()
    {
        // Arrange
        var logger = CreateLogger(out var fs, out var time, "/test/hits.csv");
        time.SetTime(new DateTime(2026, 2, 25, 10, 30, 0, DateTimeKind.Utc));

        // Act
        await logger.LogHitAsync(154.2800, "Dispatch", "FDNY");

        // Assert
        Assert.True(fs.FileExists("/test/hits.csv"));
        string contents = fs.Files["/test/hits.csv"];
        Assert.Contains("154.2800", contents);
        Assert.Contains("Dispatch", contents);
        Assert.Contains("FDNY", contents);
        Assert.Contains("2026-02-25 10:30:00", contents);
    }

    [Fact]
    public async Task LogHitAsync_SkipsZeroFrequency()
    {
        // Arrange
        var logger = CreateLogger(out var fs, out var time);

        // Act
        await logger.LogHitAsync(0, "Dispatch", "FDNY");

        // Assert
        Assert.False(fs.FileExists("/test/log.csv"), "Should not create file for zero frequency");
    }

    [Fact]
    public async Task LogHitAsync_SkipsEmptyChannel()
    {
        // Arrange
        var logger = CreateLogger(out var fs, out var time);

        // Act
        await logger.LogHitAsync(154.28, "", "FDNY");

        // Assert
        Assert.False(fs.FileExists("/test/log.csv"));
    }

    [Fact]
    public async Task LogHitAsync_SkipsDefaultChannelEllipsis()
    {
        // Arrange
        var logger = CreateLogger(out var fs, out var time);

        // Act
        await logger.LogHitAsync(154.28, "...", "FDNY");

        // Assert
        Assert.False(fs.FileExists("/test/log.csv"), "Should skip '...' placeholder channels");
    }

    [Fact]
    public async Task LogHitAsync_AppendsMultipleEntries()
    {
        // Arrange
        var logger = CreateLogger(out var fs, out var time, "/test/multi.csv");

        // Act
        await logger.LogHitAsync(154.28, "ChannelA", "SysA");
        await logger.LogHitAsync(863.56, "ChannelB", "SysB");

        // Assert
        Assert.Equal(2, fs.AppendLog["/test/multi.csv"].Count);
        string all = fs.Files["/test/multi.csv"];
        Assert.Contains("ChannelA", all);
        Assert.Contains("ChannelB", all);
    }

    [Fact]
    public void LogPath_ReturnsInjectedPath()
    {
        // Arrange
        var logger = CreateLogger(out _, out _, "/custom/path.csv");

        // Assert
        Assert.Equal("/custom/path.csv", logger.LogPath);
    }
}

