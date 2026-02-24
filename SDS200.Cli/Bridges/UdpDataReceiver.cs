using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;

/// <summary>
/// UDP-specific data receiver that handles the SDS200 UDP protocol requirements:
/// - Single-packet responses for most commands (MDL, GSI, PSI, etc.)
/// - Multi-packet assembly for GLT (Get List) commands only
/// - GLT uses packet sequencing via Footer No/EOT attributes
/// </summary>
public sealed partial class UdpDataReceiver : IDataReceiver
{
    private readonly UdpClient _client;
    private TaskCompletionSource<string>? _responseTcs;
    private bool _expectingXmlResponse;
    private readonly object _lock = new();
    
    // Multi-packet XML assembly state
    private readonly StringBuilder _xmlBuffer = new();
    private int _lastPacketNumber;
    private string? _xmlCommandPrefix;

    public event Action<string>? OnMessageReceived;

    public UdpDataReceiver(UdpClient client)
    {
        _client = client;
    }

    public void ExpectResponse(TaskCompletionSource<string> tcs, bool isXmlCommand)
    {
        lock (_lock)
        {
            _responseTcs = tcs;
            _expectingXmlResponse = isXmlCommand;
            
            if (isXmlCommand)
            {
                _xmlBuffer.Clear();
                _lastPacketNumber = 0;
                _xmlCommandPrefix = null;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _client.ReceiveAsync(cancellationToken);
                var message = Encoding.ASCII.GetString(result.Buffer).Trim();

                if (string.IsNullOrEmpty(message))
                    continue;

                ProcessMessage(message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                // Brief delay to prevent tight error loop
                await Task.Delay(250, cancellationToken);
            }
        }
    }

    private void ProcessMessage(string message)
    {
        lock (_lock)
        {
            if (_expectingXmlResponse && IsXmlPacket(message))
            {
                ProcessXmlPacket(message);
            }
            else
            {
                // Simple single-packet response
                OnMessageReceived?.Invoke(message);
                _responseTcs?.TrySetResult(message);
            }
        }
    }

    private void ProcessXmlPacket(string packet)
    {
        // Extract the command prefix (e.g., "GLT" from "GLT,<XML>,...")
        if (_xmlCommandPrefix == null)
        {
            var commaIndex = packet.IndexOf(',');
            if (commaIndex > 0)
            {
                _xmlCommandPrefix = packet[..commaIndex];
            }
        }

        // Check for Footer to determine packet number and EOT status
        var footerMatch = FooterRegex().Match(packet);
        if (footerMatch.Success)
        {
            var packetNumber = int.Parse(footerMatch.Groups["no"].Value);
            var isEndOfTransmission = footerMatch.Groups["eot"].Value == "1";

            // Detect packet loss
            if (packetNumber != _lastPacketNumber + 1)
            {
                // Packet loss detected - for now, log and continue
                // A more robust implementation could request retransmission
                System.Diagnostics.Debug.WriteLine($"UDP packet loss detected: expected {_lastPacketNumber + 1}, got {packetNumber}");
            }
            _lastPacketNumber = packetNumber;

            // Extract XML content from packet (between <XML>, and <Footer...)
            var xmlContent = ExtractXmlContent(packet);
            _xmlBuffer.Append(xmlContent);

            if (isEndOfTransmission)
            {
                // Complete XML response assembled
                var fullResponse = $"{_xmlCommandPrefix},{_xmlBuffer}";
                OnMessageReceived?.Invoke(fullResponse);
                _responseTcs?.TrySetResult(fullResponse);
                
                // Reset state
                _xmlBuffer.Clear();
                _lastPacketNumber = 0;
                _xmlCommandPrefix = null;
                _expectingXmlResponse = false;
            }
        }
        else
        {
            // Packet without Footer - might be first packet with command echo
            // Just accumulate content
            var xmlContent = ExtractXmlContent(packet);
            _xmlBuffer.Append(xmlContent);
        }
    }

    private static bool IsXmlPacket(string message)
    {
        // XML packets typically start with command,<XML>, or contain XML declaration
        return message.Contains("<XML>", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("<?xml", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("<Footer", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractXmlContent(string packet)
    {
        // Remove the command prefix (e.g., "GLT,<XML>," or "GLT,")
        var xmlStart = packet.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (xmlStart >= 0)
        {
            // Find the end - exclude Footer element for assembly
            var footerStart = packet.IndexOf("<Footer", StringComparison.OrdinalIgnoreCase);
            if (footerStart > xmlStart)
            {
                return packet[xmlStart..footerStart];
            }
            return packet[xmlStart..];
        }

        // If no XML declaration, try to extract content after comma
        var commaIndex = packet.IndexOf(',');
        if (commaIndex >= 0)
        {
            var content = packet[(commaIndex + 1)..];
            // Remove Footer if present
            var footerStart = content.IndexOf("<Footer", StringComparison.OrdinalIgnoreCase);
            if (footerStart >= 0)
            {
                return content[..footerStart];
            }
            return content;
        }

        return packet;
    }

    [GeneratedRegex(@"<Footer\s+No=""(?<no>\d+)""\s+EOT=""(?<eot>[01])""\s*/>", RegexOptions.IgnoreCase)]
    private static partial Regex FooterRegex();
}

