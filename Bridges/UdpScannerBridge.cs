namespace SdsRemote.Bridges;

using System.Net.Sockets;
using System.Text;
using SdsRemote.Core;

public class UdpScannerBridge : IScannerBridge
{
    private UdpClient? _client;
    private bool _running;
    private TaskCompletionSource<string>? _responseTcs;
    private readonly object _lock = new();

    public bool IsConnected { get; private set; }
    public event Action<string>? OnDataReceived;

    public async Task ConnectAsync(string ip, int port)
    {
        _client = new UdpClient();
        _client.Connect(ip, port);
        IsConnected = true;
        _running = true;
        _ = Task.Run(ReceiveLoop);
    }

    private async Task ReceiveLoop()
    {
        while (_running && _client != null)
        {
            try
            {
                var result = await _client.ReceiveAsync();
                var message = Encoding.ASCII.GetString(result.Buffer).Trim();

                if (!string.IsNullOrEmpty(message))
                {
                    OnDataReceived?.Invoke(message);

                    // If a command is waiting for a response, fulfill it
                    lock (_lock)
                    {
                        if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
                        {
                            _responseTcs.TrySetResult(message);
                        }
                    }
                }
            }
            catch { IsConnected = false; }
        }
    }

    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        if (_client == null) return "DISCONNECTED";

        lock (_lock)
        {
            _responseTcs = new TaskCompletionSource<string>();
        }

        byte[] bytes = Encoding.ASCII.GetBytes(command.ToUpper().Trim() + "\r");
        await _client.SendAsync(bytes, bytes.Length);

        // Wait for the specific response or the timeout
        var completedTask = await Task.WhenAny(_responseTcs.Task, Task.Delay(timeout));

        if (completedTask == _responseTcs.Task)
        {
            return await _responseTcs.Task;
        }

        return "TIMEOUT";
    }

    public async Task SendCommandAsync(string cmd)
    {
        if (_client == null) return;
        byte[] bytes = Encoding.ASCII.GetBytes(cmd.ToUpper().Trim() + "\r");
        await _client.SendAsync(bytes, bytes.Length);
    }

    public void Dispose()
    {
        _running = false;
        _client?.Dispose();
    }
}