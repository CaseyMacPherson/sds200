namespace SdsRemote.Tests;

using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;
using SDS200.Cli.Bridges;

/// <summary>
/// Tests for UdpDataReceiver multi-packet XML response handling.
/// Validates compliance with the UDP Network Protocol specification.
/// </summary>
public class UdpDataReceiverTests
{
    /// <summary>
    /// Simulates the UDP protocol packet format from the spec.
    /// </summary>
    private static string CreateXmlPacket(string command, string xmlContent, int packetNumber, bool isEndOfTransmission)
    {
        var eot = isEndOfTransmission ? "1" : "0";
        return $"{command},<XML>,\n<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<{command}>\n{xmlContent}\n<Footer No=\"{packetNumber}\" EOT=\"{eot}\"/>";
    }

    [Fact]
    public async Task SinglePacketResponse_CompletesImmediately()
    {
        // Arrange - Set up a local UDP server to simulate scanner
        using var scanner = new UdpClient(0);
        
        using var client = new UdpClient(0);
        var clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;
        var clientEndPoint = new IPEndPoint(IPAddress.Loopback, clientPort);

        var receiver = new UdpDataReceiver(client);
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ExpectResponse(tcs, isXmlCommand: false);

        using var cts = new CancellationTokenSource();
        _ = receiver.StartAsync(cts.Token);

        // Act - Simulate scanner sending simple response
        var response = "MDL,SDS200";
        var bytes = Encoding.ASCII.GetBytes(response);
        await scanner.SendAsync(bytes, bytes.Length, clientEndPoint);

        // Assert
        var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Equal(tcs.Task, result);
        Assert.Equal(response, await tcs.Task);

        cts.Cancel();
    }

    [Fact]
    public async Task MultiPacketXmlResponse_AssemblesAllPackets()
    {
        // Arrange
        using var scanner = new UdpClient(0);
        
        using var client = new UdpClient(0);
        var clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;
        var clientEndPoint = new IPEndPoint(IPAddress.Loopback, clientPort);

        var receiver = new UdpDataReceiver(client);
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ExpectResponse(tcs, isXmlCommand: true);

        using var cts = new CancellationTokenSource();
        _ = receiver.StartAsync(cts.Token);

        // Act - Simulate scanner sending multi-packet GLT response (as per spec)
        var packet1 = CreateXmlPacket("GLT", "<FL Index=\"1\" Name=\"FL 1\"/>", 1, false);
        var packet2 = CreateXmlPacket("GLT", "<FL Index=\"2\" Name=\"FL 2\"/>", 2, false);
        var packet3 = CreateXmlPacket("GLT", "<FL Index=\"3\" Name=\"FL 3\"/>", 3, true); // EOT=1

        await SendPacketAsync(scanner, clientEndPoint, packet1);
        await Task.Delay(50); // Small delay between packets
        await SendPacketAsync(scanner, clientEndPoint, packet2);
        await Task.Delay(50);
        await SendPacketAsync(scanner, clientEndPoint, packet3);

        // Assert
        var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Equal(tcs.Task, result);
        
        var response = await tcs.Task;
        Assert.StartsWith("GLT,", response);
        // Should contain content from all packets
        Assert.Contains("FL 1", response);
        Assert.Contains("FL 2", response);
        Assert.Contains("FL 3", response);

        cts.Cancel();
    }

    [Fact]
    public async Task XmlResponse_WithFooterEot1_CompletesOnSinglePacket()
    {
        // Arrange
        using var scanner = new UdpClient(0);
        
        using var client = new UdpClient(0);
        var clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;
        var clientEndPoint = new IPEndPoint(IPAddress.Loopback, clientPort);

        var receiver = new UdpDataReceiver(client);
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ExpectResponse(tcs, isXmlCommand: true);

        using var cts = new CancellationTokenSource();
        _ = receiver.StartAsync(cts.Token);

        // Act - Single packet with EOT=1
        var packet = CreateXmlPacket("GSI", "<ScannerInfo Model=\"SDS200\"/>", 1, true);
        await SendPacketAsync(scanner, clientEndPoint, packet);

        // Assert
        var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Equal(tcs.Task, result);
        
        var response = await tcs.Task;
        Assert.StartsWith("GSI,", response);
        Assert.Contains("ScannerInfo", response);

        cts.Cancel();
    }

    [Fact]
    public void FooterRegex_ParsesCorrectly()
    {
        // Test the regex pattern used for parsing Footer elements
        var pattern = @"<Footer\s+No=""(?<no>\d+)""\s+EOT=""(?<eot>[01])""\s*/>";
        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        var testInput = "<Footer No=\"3\" EOT=\"1\"/>";
        var match = regex.Match(testInput);
        
        Assert.True(match.Success);
        Assert.Equal("3", match.Groups["no"].Value);
        Assert.Equal("1", match.Groups["eot"].Value);
    }

    [Fact]
    public void FooterRegex_ParsesWithDifferentSpacing()
    {
        var pattern = @"<Footer\s+No=""(?<no>\d+)""\s+EOT=""(?<eot>[01])""\s*/>";
        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Test with extra spaces
        var testInput = "<Footer  No=\"42\"  EOT=\"0\"  />";
        var match = regex.Match(testInput);
        
        Assert.True(match.Success);
        Assert.Equal("42", match.Groups["no"].Value);
        Assert.Equal("0", match.Groups["eot"].Value);
    }

    private static async Task SendPacketAsync(UdpClient sender, IPEndPoint target, string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        await sender.SendAsync(bytes, bytes.Length, target);
    }
}

