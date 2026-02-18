namespace SdsRemote.Tests;

using Xunit;
using Moq;
using SDS200.Cli.Core;
using SDS200.Cli.Models;

public class ScannerBridgeTests
{
    [Fact]
    public async Task ConnectAsync_IsCalledWithCorrectPort()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        bridgeMock
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await bridgeMock.Object.ConnectAsync("192.168.1.100", 50536);

        // Assert
        bridgeMock.Verify(x => x.ConnectAsync("192.168.1.100", 50536), Times.Once);
    }

    [Fact]
    public async Task SendAndReceiveAsync_ReturnsResponse()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        string expectedResponse = GsiTestData.ConventionalScanXml;
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(expectedResponse, result);
        bridgeMock.Verify(x => x.SendAndReceiveAsync("GSI,0", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task SendCommandAsync_Called()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        bridgeMock
            .Setup(x => x.SendCommandAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await bridgeMock.Object.SendCommandAsync("RI");

        // Assert
        bridgeMock.Verify(x => x.SendCommandAsync("RI"), Times.Once);
    }

    [Fact]
    public void IsConnected_ReportsConnectionStatus()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        bridgeMock.Setup(x => x.IsConnected).Returns(true);

        // Act
        var connected = bridgeMock.Object.IsConnected;

        // Assert
        Assert.True(connected);
    }

    [Fact]
    public void OnDataReceived_EventCanBeSent()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        string? receivedData = null;
        bridgeMock.Object.OnDataReceived += (data) => receivedData = data;

        // Act
        bridgeMock.Raise(x => x.OnDataReceived += null, GsiTestData.ConventionalScanXml);

        // Assert
        Assert.Equal(GsiTestData.ConventionalScanXml, receivedData);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WithTimeout()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        var timeout = TimeSpan.FromMilliseconds(500);
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync("GSI,0", timeout))
            .ReturnsAsync("TIMEOUT");

        // Act
        var result = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", timeout);

        // Assert
        Assert.Equal("TIMEOUT", result);
    }

    [Fact]
    public async Task MultipleCommands_SentInSequence()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        var results = new[] { GsiTestData.ConventionalScanXml, GsiTestData.TrunkScanXml };
        var queue = new Queue<string>(results);
        
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));

        // Act
        var result1 = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
        var result2 = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(GsiTestData.ConventionalScanXml, result1);
        Assert.Equal(GsiTestData.TrunkScanXml, result2);
    }

    [Fact]
    public void OnDataReceived_MultipleSubscribers()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        var received1 = false;
        var received2 = false;

        bridgeMock.Object.OnDataReceived += (_) => received1 = true;
        bridgeMock.Object.OnDataReceived += (_) => received2 = true;

        // Act
        bridgeMock.Raise(x => x.OnDataReceived += null, "test data");

        // Assert
        Assert.True(received1);
        Assert.True(received2);
    }
}
