namespace SdsRemote.Tests;

using Xunit;
using Moq;
using SdsRemote.Core;
using SdsRemote.Models;
using SdsRemote.Logic;

public class IntegrationTests
{
    [Fact]
    public async Task ParserWithBridgeMock_ProcessesGsiResponse()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        var status = new ScannerStatus();
        
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync("GSI,0", It.IsAny<TimeSpan>()))
            .ReturnsAsync(GsiTestData.ConventionalScanXml);

        // Act
        var response = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
        bool parsed = UnidenParser.UpdateStatus(status, response);

        // Assert
        Assert.True(parsed);
        Assert.Equal("FDNY", status.SystemName);
        Assert.Equal(154.2800d, status.Frequency);
    }

    [Fact]
    public async Task ContactLogEntry_CreatedFromParsedStatus()
    {
        // Arrange
        var status = new ScannerStatus();
        UnidenParser.UpdateStatus(status, GsiTestData.TrunkScanXml);
        
        // Act
        var contact = ContactLogEntry.FromStatus(status);

        // Assert
        Assert.NotNull(contact);
        Assert.Equal("Police RAN", contact.SystemName);
        Assert.Equal("trunk_scan", contact.Mode);
        Assert.Equal("1234", contact.TgId);
    }

    [Fact]
    public void SignalLockTracking_LockOnHighRssi()
    {
        // Arrange
        var status = new ScannerStatus();
        UnidenParser.UpdateStatus(status, GsiTestData.ConventionalScanXml); // Rssi=3

        // Act
        bool wasLocked = status.SignalLocked;
        status.SignalLocked = status.LastRssiValue > 0;

        // Assert
        Assert.True(status.SignalLocked);
        Assert.True(status.LastRssiValue > 0);
    }

    [Fact]
    public void SignalLockTracking_LockOnZeroRssi()
    {
        // Arrange
        var status = new ScannerStatus();
        UnidenParser.UpdateStatus(status, GsiTestData.NoSignalXml); // Rssi=0

        // Act
        status.SignalLocked = status.LastRssiValue > 0;

        // Assert
        Assert.False(status.SignalLocked);
        Assert.Equal(0, status.LastRssiValue);
    }

    [Fact]
    public async Task SequentialParsing_MultipleResponses()
    {
        // Arrange
        var responses = new[] 
        { 
            GsiTestData.ConventionalScanXml,
            GsiTestData.TrunkScanXml,
            GsiTestData.ToneOutXml,
        };

        var status = new ScannerStatus();
        var statuses = new List<string>();

        // Act
        foreach (var response in responses)
        {
            bool parsed = UnidenParser.UpdateStatus(status, response);
            if (parsed)
                statuses.Add(status.VScreen);
        }

        // Assert
        Assert.Equal(3, statuses.Count);
        Assert.Equal("conventional_scan", statuses[0]);
        Assert.Equal("trunk_scan", statuses[1]);
        Assert.Equal("tone_out", statuses[2]);
    }

    [Fact]
    public void ContactLog_QueueManagement()
    {
        // Arrange
        var contacts = new Queue<ContactLogEntry>();
        var status = new ScannerStatus();

        // Act - Simulate 35 contacts
        for (int i = 0; i < 35; i++)
        {
            status.Frequency = 150.0d + (i * 0.1);
            status.SystemName = $"System_{i}";
            var entry = ContactLogEntry.FromStatus(status);
            contacts.Enqueue(entry);
            
            // Keep only 30 as in main program
            if (contacts.Count > 30) 
                contacts.Dequeue();
        }

        // Assert
        Assert.Equal(30, contacts.Count);
        Assert.Equal("System_5", contacts.Peek().SystemName); // First one kept
        Assert.Equal("System_34", contacts.Last().SystemName); // Last one added
    }

    [Fact]
    public void MalformedData_DoesNotCorruptStatus()
    {
        // Arrange
        var status = new ScannerStatus();
        status.SystemName = "Original System";
        string malformed = GsiTestData.InvalidXml;

        // Act
        bool parsed = UnidenParser.UpdateStatus(status, malformed);

        // Assert
        Assert.False(parsed);
        Assert.Equal("Original System", status.SystemName); // Unchanged on parse failure
    }

    [Fact]
    public async Task BridgeResponseFlow_ToParserToModel()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync("GSI,0", It.IsAny<TimeSpan>()))
            .ReturnsAsync(GsiTestData.DiscoveryTrunkingXml);

        var status = new ScannerStatus();

        // Act
        var response = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
        var parsed = UnidenParser.UpdateStatus(status, response);

        // Assert
        Assert.True(parsed);
        Assert.Equal("Rail System", status.SystemName);
        Assert.Equal("5001", status.TgId);
        Assert.Equal(12, status.HitCount);
    }

    [Fact]
    public void PropertyParsing_VolumeSquelch()
    {
        // Arrange
        var status = new ScannerStatus();
        
        // Act
        UnidenParser.UpdateStatus(status, GsiTestData.ConventionalScanXml);

        // Assert
        Assert.Equal(15, status.Volume);
        Assert.Equal(10, status.Squelch);
    }

    [Fact]
    public async Task ErrorRecovery_AfterFailedParse()
    {
        // Arrange
        var bridgeMock = new Mock<IScannerBridge>();
        var responses = new Queue<string>(new[] 
        { 
            GsiTestData.InvalidXml,  // First response fails
            GsiTestData.ConventionalScanXml // Second succeeds
        });
        
        bridgeMock
            .Setup(x => x.SendAndReceiveAsync("GSI,0", It.IsAny<TimeSpan>()))
            .Returns(() => Task.FromResult(responses.Dequeue()));

        var status = new ScannerStatus();

        // Act - First attempt fails
        var badResponse = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
        bool badParsed = UnidenParser.UpdateStatus(status, badResponse);

        // Act - Second attempt succeeds
        var goodResponse = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
        bool goodParsed = UnidenParser.UpdateStatus(status, goodResponse);

        // Assert
        Assert.False(badParsed);
        Assert.True(goodParsed);
        Assert.Equal("FDNY", status.SystemName);
    }
}
