using System.IO.Ports;
using System.Text;
using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;

/// <summary>
/// Serial-specific data receiver that handles stream-based buffering.
/// Accumulates characters until the \r delimiter is received.
/// </summary>
public sealed class SerialDataReceiver : IDataReceiver
{
    private readonly SerialPort _port;
    private readonly StringBuilder _buffer = new();
    private TaskCompletionSource<string>? _responseTcs;
    private readonly object _lock = new();
    private bool _running;

    public event Action<string>? OnMessageReceived;

    public SerialDataReceiver(SerialPort port)
    {
        _port = port;
    }

    public void ExpectResponse(TaskCompletionSource<string> tcs, bool isXmlCommand)
    {
        // Serial protocol doesn't need special handling for XML commands
        // as responses still arrive character-by-character with \r delimiters
        lock (_lock)
        {
            _responseTcs = tcs;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _running = true;
        
        _port.DataReceived += (_, _) =>
        {
            if (!_running) return;
            
            var chunk = _port.ReadExisting();
            ProcessChunk(chunk);
        };

        // Register cancellation
        cancellationToken.Register(() =>
        {
            _running = false;
        });

        return Task.CompletedTask;
    }

    private void ProcessChunk(string chunk)
    {
        lock (_lock)
        {
            foreach (var c in chunk)
            {
                if (c == '\r')
                {
                    var fullLine = _buffer.ToString();
                    _buffer.Clear();

                    OnMessageReceived?.Invoke(fullLine);
                    _responseTcs?.TrySetResult(fullLine);
                }
                else
                {
                    _buffer.Append(c);
                }
            }
        }
    }
}

