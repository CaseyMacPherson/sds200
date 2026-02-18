namespace SdsRemote.Bridges;

using System.Net;
using System.Net.Sockets;
using System.Text;
using SdsRemote.Core;

public class UdpScannerBridge : IScannerBridge
{
    private UdpClient? _client;
    private IPEndPoint? _remoteEndPoint;
    private bool _running;
    private TaskCompletionSource<string>? _responseTcs;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;

    public bool IsConnected { get; private set; }
    public event Action<string>? OnDataReceived;

    public async Task ConnectAsync(string ip, int port)
    {
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        // Use an unconnected UDP client — avoids macOS/Linux platform quirks
        // where "connected" UDP sockets filter ReceiveAsync incorrectly.
        _client = new UdpClient(0); // Bind to any available local port
        _cts = new CancellationTokenSource();
        _running = true;

        // Start the background receive loop
        _ = Task.Run(ReceiveLoop);

        // Verify the scanner is reachable with a simple MDL probe
        var probe = await SendAndReceiveAsync("MDL", TimeSpan.FromSeconds(2));
        IsConnected = probe != "TIMEOUT" && probe != "DISCONNECTED";
    }

    private async Task ReceiveLoop()
    {
        while (_running && _client != null)
        {
            try
            {
                var result = await _client.ReceiveAsync(_cts!.Token);
                var message = Encoding.ASCII.GetString(result.Buffer).Trim();

                if (!string.IsNullOrEmpty(message))
                {
                    OnDataReceived?.Invoke(message);

                    // If a command is waiting for a response, fulfill it
                    lock (_lock)
                    {
                        _responseTcs?.TrySetResult(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown via CancellationToken
                break;
            }
            catch (ObjectDisposedException)
            {
                // Socket was disposed, exit cleanly
                break;
            }
            catch (SocketException)
            {
                IsConnected = false;
                // Brief delay to prevent tight error loop
                await Task.Delay(250);
            }
        }
    }

    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        if (_client == null || _remoteEndPoint == null) return "DISCONNECTED";

        // Capture a local reference so a concurrent call can't swap the TCS out from under us
        TaskCompletionSource<string> tcs;
        lock (_lock)
        {
            tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _responseTcs = tcs;
        }

        byte[] bytes = Encoding.ASCII.GetBytes(command.ToUpper().Trim() + "\r");
        await _client.SendAsync(bytes, bytes.Length, _remoteEndPoint);

        // Wait for the specific response or the timeout
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

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
        byte[] bytes = Encoding.ASCII.GetBytes(cmd.ToUpper().Trim() + "\r");
        await _client.SendAsync(bytes, bytes.Length, _remoteEndPoint);
    }

    public void Dispose()
    {
        _running = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _client?.Dispose();
    }
}