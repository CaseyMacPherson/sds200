namespace SdsRemote.Tests;

using System.Collections.Concurrent;
using Xunit;
using Moq;
using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Logic;
using SDS200.Cli.Tests;

public class GsiResponseHandlerTests
{
    private static (GsiResponseHandler handler, ScannerStatus status, Queue<string> debugLog, ConcurrentQueue<string> rawData)
        CreateHandler(IResponseParser? parser = null, ITimeProvider? time = null)
    {
        var status = new ScannerStatus();
        var debugLog = new Queue<string>();
        var rawData = new ConcurrentQueue<string>();
        var handler = new GsiResponseHandler(status, debugLog, rawData, parser, time ?? new FakeTimeProvider());
        return (handler, status, debugLog, rawData);
    }

    [Fact]
    public void OnDataReceived_PartialXml_BuffersWithoutParsing()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        var (handler, _, _, _) = CreateHandler(parser: parserMock.Object);

        // Act — send only part of the document
        handler.OnDataReceived("<ScannerInfo Mode");

        // Assert — parser should NOT be called yet
        parserMock.Verify(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("<ScannerInfo Mode", handler.GetBufferContents());
    }

    [Fact]
    public void OnDataReceived_CompleteDocument_CallsParser()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        parserMock.Setup(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>())).Returns(true);
        var (handler, _, _, _) = CreateHandler(parser: parserMock.Object);

        // Act
        handler.OnDataReceived(GsiTestData.ConventionalScanXml);

        // Assert
        parserMock.Verify(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void OnDataReceived_CompleteDocument_ClearsBufferAfterParse()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        parserMock.Setup(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>())).Returns(true);
        var (handler, _, _, _) = CreateHandler(parser: parserMock.Object);

        // Act
        handler.OnDataReceived(GsiTestData.ConventionalScanXml);

        // Assert
        Assert.Equal("", handler.GetBufferContents());
    }

    [Fact]
    public void OnDataReceived_SplitAcrossChunks_AssemblesAndParses()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        parserMock.Setup(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>())).Returns(true);
        var (handler, _, _, _) = CreateHandler(parser: parserMock.Object);
        string[] chunks = SplitInHalf(GsiTestData.ConventionalScanXml);

        // Act
        handler.OnDataReceived(chunks[0]);
        parserMock.Verify(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>()), Times.Never,
            "Parser must not fire on first partial chunk");

        handler.OnDataReceived(chunks[1]);

        // Assert
        parserMock.Verify(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void OnDataReceived_ParseFail_LogsFailureToDebugLog()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        parserMock.Setup(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>())).Returns(false);
        var (handler, _, debugLog, _) = CreateHandler(parser: parserMock.Object);

        // Act
        handler.OnDataReceived(GsiTestData.ConventionalScanXml);

        // Assert
        Assert.Single(debugLog);
        Assert.Contains("Parse failed", debugLog.Peek());
    }

    [Fact]
    public void OnDataReceived_ParseSuccess_LogsSuccessToDebugLog()
    {
        // Arrange
        var parserMock = new Mock<IResponseParser>();
        parserMock.Setup(p => p.UpdateStatus(It.IsAny<ScannerStatus>(), It.IsAny<string>())).Returns(true);
        var (handler, _, debugLog, _) = CreateHandler(parser: parserMock.Object);

        // Act
        handler.OnDataReceived(GsiTestData.ConventionalScanXml);

        // Assert
        Assert.Single(debugLog);
        Assert.Contains("GSI parsed", debugLog.Peek());
    }

    [Fact]
    public void OnDataSent_LogsToRawRadioData()
    {
        // Arrange
        var (handler, _, _, rawData) = CreateHandler();

        // Act
        handler.OnDataSent("GSI,0");

        // Assert
        Assert.Single(rawData);
        Assert.Contains(">> GSI,0", rawData.ToArray()[0]);
    }

    [Fact]
    public void OnDataReceived_LogsToRawRadioData()
    {
        // Arrange
        var (handler, _, _, rawData) = CreateHandler();

        // Act — send a partial (no </ScannerInfo>) so only logging occurs
        handler.OnDataReceived("some data");

        // Assert
        Assert.Single(rawData);
        Assert.Contains("<< some data", rawData.ToArray()[0]);
    }

    [Fact]
    public void ClearBuffer_EmptiesAccumulatedData()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler();
        handler.OnDataReceived("partial xml");

        // Act
        handler.ClearBuffer();

        // Assert
        Assert.Equal("", handler.GetBufferContents());
    }

    [Fact]
    public void RawRadioData_RespectsMaxSize()
    {
        // Arrange
        const int maxSize = 3;
        var status = new ScannerStatus();
        var rawData = new ConcurrentQueue<string>();
        var handler = new GsiResponseHandler(status, new Queue<string>(), rawData,
            timeProvider: new FakeTimeProvider(), maxRawDataSize: maxSize);

        // Act — send 5 entries
        for (int i = 0; i < 5; i++)
            handler.OnDataSent($"CMD{i}");

        // Assert
        Assert.Equal(maxSize, rawData.Count);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static string[] SplitInHalf(string s)
    {
        int mid = s.Length / 2;
        return [s[..mid], s[mid..]];
    }
}

