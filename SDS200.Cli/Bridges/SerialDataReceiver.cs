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
    private readonly StringBuilder _xmlAccumulator = new();
    private TaskCompletionSource<string>? _responseTcs;
    private bool _expectingXml;
    private readonly object _lock = new();
    private bool _running;

    public event Action<string>? OnMessageReceived;

    public SerialDataReceiver(SerialPort port)
    {
        _port = port;
    }

    public void ExpectResponse(TaskCompletionSource<string> tcs, bool isXmlCommand)
    {
        lock (_lock)
        {
            _responseTcs = tcs;
            _expectingXml = isXmlCommand;
            if (isXmlCommand)
            {
                _xmlAccumulator.Clear();
            }
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

                    if (_expectingXml && _responseTcs != null)
                    {
                        // Accumulate lines until the complete XML document arrives
                        _xmlAccumulator.AppendLine(fullLine);

                        if (fullLine.Contains("</ScannerInfo>", StringComparison.OrdinalIgnoreCase))
                        {
                            var completeResponse = _xmlAccumulator.ToString().TrimEnd();
                            _xmlAccumulator.Clear();
                            _expectingXml = false;
                            _responseTcs.TrySetResult(completeResponse);
                        }
                    }
                    else
                    {
                        _responseTcs?.TrySetResult(fullLine);
                    }
                }
                else
                {
                    _buffer.Append(c);
                }
            }
        }
    }
}

