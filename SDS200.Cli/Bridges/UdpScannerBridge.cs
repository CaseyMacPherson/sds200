
using SDS200.Cli.Core;

namespace SDS200.Cli.Bridges;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP scanner bridge for SDS200 communication over network.
/// Uses UDP port 50536 (compatible with BCD536HP).
/// 
/// Key protocol differences from Serial:
/// - UDP packets are self-contained (no stream buffering needed)
/// - XML responses (GLT, GSI, PSI) are split into multiple packets
/// - Multi-packet responses use Footer No/EOT attributes for sequencing
/// </summary>
public class UdpScannerBridge : IScannerBridge
{
    /// <summary>
    /// Default UDP port for SDS200 virtual serial over network.
    /// </summary>
    public const int DefaultPort = 50536;

    private UdpClient? _client;
    private IPEndPoint? _remoteEndPoint;
    private UdpDataReceiver? _dataReceiver;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Commands that return multi-packet XML responses with Footer elements.
    /// Per the UDP Network Protocol spec, only GLT commands use multi-packet format.
    /// GSI/PSI/MSI return single-packet XML responses.
    /// </summary>
    private static readonly HashSet<string> MultiPacketXmlCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "GLT",  // Get List (Favorites, Systems, Departments) - uses Footer No/EOT
    };

    public bool IsConnected { get; private set; }
    public event Action<string>? OnDataReceived;
    public event Action<string>? OnDataSent;

    public async Task ConnectAsync(string ip, int port)
    {
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        // Use an unconnected UDP client — avoids macOS/Linux platform quirks
        // where "connected" UDP sockets filter ReceiveAsync incorrectly.
        _client = new UdpClient(0); // Bind to any available local port
        _cts = new CancellationTokenSource();

        // Create the UDP-specific data receiver for protocol handling
        _dataReceiver = new UdpDataReceiver(_client);
        _dataReceiver.OnMessageReceived += message =>
        {
            OnDataReceived?.Invoke(message);
        };

        // Start the background receive loop
        _ = Task.Run(() => _dataReceiver.StartAsync(_cts.Token));

        // Verify the scanner is reachable with a simple MDL probe
        var probe = await SendAndReceiveAsync("MDL", TimeSpan.FromSeconds(2));
        IsConnected = probe != "TIMEOUT" && probe != "DISCONNECTED";
    }

    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        if (_client == null || _remoteEndPoint == null || _dataReceiver == null) 
            return "DISCONNECTED";

        var normalizedCommand = command.ToUpper().Trim();
        var isMultiPacketXml = IsMultiPacketXmlCommand(normalizedCommand);
        
        // Use longer timeout for multi-packet XML commands (GLT) as they involve multiple packets
        var effectiveTimeout = isMultiPacketXml ? TimeSpan.FromSeconds(Math.Max(timeout.TotalSeconds, 10)) : timeout;

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dataReceiver.ExpectResponse(tcs, isMultiPacketXml);

        var bytes = Encoding.ASCII.GetBytes(normalizedCommand + "\r");
        OnDataSent?.Invoke(normalizedCommand);
        await _client.SendAsync(bytes, bytes.Length, _remoteEndPoint);

        // Wait for the complete response or timeout
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(effectiveTimeout));

        if (completedTask == tcs.Task)
        {
            IsConnected = true; // Got a response — scanner is alive
            return await tcs.Task;
        }

        return "TIMEOUT";
    }

    public async Task SendCommandAsync(string cmd)
    {
        if (_client == null || _remoteEndPoint == null) return;
        var normalizedCmd = cmd.ToUpper().Trim();
        var bytes = Encoding.ASCII.GetBytes(normalizedCmd + "\r");
        OnDataSent?.Invoke(normalizedCmd);
        await _client.SendAsync(bytes, bytes.Length, _remoteEndPoint);
    }

    /// <summary>
    /// Determines if a command returns multi-packet XML responses with Footer elements.
    /// </summary>
    private static bool IsMultiPacketXmlCommand(string command)
    {
        // Extract the base command (before any parameters)
        var baseCommand = command.Split(',')[0].Trim();
        return MultiPacketXmlCommands.Contains(baseCommand);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _client?.Dispose();
    }
}