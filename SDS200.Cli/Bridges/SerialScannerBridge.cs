using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;
using System.IO.Ports;
using System.Text;

public class SerialScannerBridge : IScannerBridge
{
    private SerialPort? _port;
    private StringBuilder _buffer = new();
    private TaskCompletionSource<string>? _responseTcs;
    private bool _eventMonitoringEnabled;
    public bool IsConnected => _port?.IsOpen ?? false;
    public event Action<string>? OnDataReceived;
    public event Action<string>? OnDataSent;

    public async Task ConnectAsync(string name, int baud) {
        _port = new SerialPort(name, baud, Parity.None, 8, StopBits.One) { ReadTimeout = 1000, WriteTimeout = 1000 };
        _port.DataReceived += (s, e) => {
            string chunk = _port.ReadExisting();
            foreach (char c in chunk) {
                if (c == '\r') {
                    var fullLine = _buffer.ToString();
                    _buffer.Clear();
                    _responseTcs?.TrySetResult(fullLine);
                    if (_eventMonitoringEnabled)
                        OnDataReceived?.Invoke(fullLine);
                } else {
                    _buffer.Append(c);
                }
            }
        };
        _port.Open();
        await Task.CompletedTask;
    }

    public void EnableEventMonitoring() => _eventMonitoringEnabled = true;
    public void DisableEventMonitoring() => _eventMonitoringEnabled = false;

    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout) {
        if (!IsConnected) return "";
        _responseTcs = new TaskCompletionSource<string>();
        
        var normalizedCommand = command.ToUpper();
        OnDataSent?.Invoke(normalizedCommand);
        _port!.Write(normalizedCommand + "\r");

        // Wait for the full line or timeout
        var completedTask = await Task.WhenAny(_responseTcs.Task, Task.Delay(timeout));
        return completedTask == _responseTcs.Task ? await _responseTcs.Task : "TIMEOUT";
    }

    // Standard method for one-way keys (Scan, Hold)
    public async Task SendCommandAsync(string cmd) {
        if (IsConnected) {
            var normalizedCmd = cmd.ToUpper();
            OnDataSent?.Invoke(normalizedCmd);
            _port!.Write(normalizedCmd + "\r");
        }
        await Task.CompletedTask;
    }

    public void Dispose() => _port?.Dispose();

    /// <summary>
    /// Returns available serial ports, filtered to exclude debug and Bluetooth ports.
    /// Prefers cu.* (macOS call-out) over tty.*, and prioritizes usbserial/usbmodem.
    /// </summary>
    public static string[] GetFilteredPorts()
    {
        return SerialPort.GetPortNames()
            .Where(p => !p.Contains("debug", StringComparison.OrdinalIgnoreCase) &&
                        !p.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Contains("/dev/cu.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.Contains("usbserial", StringComparison.OrdinalIgnoreCase) ||
                                    p.Contains("usbmodem", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Auto-detect the SDS200 scanner by probing each filtered port with the MDL command.
    /// Returns the port name if found, or null.
    /// </summary>
    public static async Task<string?> DetectScannerPortAsync(Action<string>? log = null)
    {
        var ports = GetFilteredPorts();
        if (ports.Length == 0)
        {
            log?.Invoke("No serial ports found (after filtering debug/Bluetooth).");
            return null;
        }

        log?.Invoke($"Probing ports: {string.Join(", ", ports)}");

        foreach (var port in ports)
        {
            try
            {
                log?.Invoke($"Testing {port}...");
                using var testPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500
                };
                testPort.Open();
                testPort.Write("MDL\r");
                await Task.Delay(500);

                var response = testPort.ReadExisting();
                testPort.Close();

                log?.Invoke($"Response from {port}: {(string.IsNullOrEmpty(response) ? "(empty)" : response.Replace("\r", "").Replace("\n", " "))}");

                if (response.Contains("SDS200", StringComparison.OrdinalIgnoreCase) ||
                    response.Contains("UNIDEN", StringComparison.OrdinalIgnoreCase))
                {
                    return port;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Error testing {port}: {ex.Message}");
            }
        }

        return null;
    }

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
}