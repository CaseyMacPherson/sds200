namespace SdsRemote.Bridges;
using System.Net.Sockets;
using System.Text;
using SdsRemote.Core;

public class UdpScannerBridge : IScannerBridge
{
    private UdpClient? _client;
    private bool _running;
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
        while (_running && _client != null) {
            try {
                var result = await _client.ReceiveAsync();
                OnDataReceived?.Invoke(Encoding.ASCII.GetString(result.Buffer));
            } catch { IsConnected = false; }
        }
    }

    public async Task SendCommandAsync(string cmd) {
        if (_client != null) await _client.SendAsync(Encoding.ASCII.GetBytes(cmd + "\r"));
    }

    public void Dispose() { _running = false; _client?.Dispose(); }
}