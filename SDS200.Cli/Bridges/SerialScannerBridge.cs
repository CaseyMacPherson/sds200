using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;
using System.IO.Ports;
using System.Text;

/// <summary>
/// Serial scanner bridge for SDS200 communication over USB.
/// Uses standard serial port settings (115200, 8N1).
/// </summary>
public class SerialScannerBridge : ScannerBridgeBase
{
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();
    private TaskCompletionSource<string>? _responseTcs;
    private bool _eventMonitoringEnabled;

    /// <summary>Gets whether the serial port is open.</summary>
    public override bool IsConnected
    {
        get => _port?.IsOpen ?? false;
        protected set { /* Read-only based on port state */ }
    }

    /// <inheritdoc/>
    public override async Task ConnectAsync(string name, int baud) {
        _port = new SerialPort(name, baud, Parity.None, 8, StopBits.One) { ReadTimeout = 1000, WriteTimeout = 1000 };
        _port.DataReceived += (s, e) => {
            string chunk = _port.ReadExisting();
            foreach (char c in chunk) {
                if (c == '\r') {
                    var fullLine = _buffer.ToString();
                    _buffer.Clear();
                    _responseTcs?.TrySetResult(fullLine);
                    if (_eventMonitoringEnabled)
                        RaiseDataReceived(fullLine);
                } else {
                    _buffer.Append(c);
                }
            }
        };
        _port.Open();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Enables event monitoring, allowing OnDataReceived to fire for all incoming data.
    /// </summary>
    public void EnableEventMonitoring() => _eventMonitoringEnabled = true;

    /// <summary>
    /// Disables event monitoring.
    /// </summary>
    public void DisableEventMonitoring() => _eventMonitoringEnabled = false;

    /// <inheritdoc/>
    protected override async Task<string> SendAndReceiveCoreAsync(string normalizedCommand, TimeSpan timeout) {
        if (!IsConnected) return "";
        _responseTcs = new TaskCompletionSource<string>();
        
        _port!.Write(normalizedCommand + "\r");

        // Wait for the full line or timeout
        var completedTask = await Task.WhenAny(_responseTcs.Task, Task.Delay(timeout));
        return completedTask == _responseTcs.Task ? await _responseTcs.Task : "TIMEOUT";
    }

    /// <inheritdoc/>
    protected override async Task SendCommandCoreAsync(string normalizedCommand) {
        if (IsConnected) {
            _port!.Write(normalizedCommand + "\r");
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override void Dispose() => _port?.Dispose();

    /// <summary>
    /// Returns available serial ports, filtered to exclude debug and Bluetooth ports.
    /// Prefers cu.* (macOS call-out) over tty.*, and prioritizes usbserial/usbmodem.
    /// </summary>
    /// <remarks>Delegates to <see cref="SerialPortHelpers.GetFilteredPorts"/>.</remarks>
    public static string[] GetFilteredPorts() => SerialPortHelpers.GetFilteredPorts();

    /// <summary>
    /// Auto-detect the SDS200 scanner by probing each filtered port with the MDL command.
    /// Returns the port name if found, or null.
    /// </summary>
    /// <remarks>Delegates to <see cref="SerialPortHelpers.DetectScannerPortAsync"/>.</remarks>
    public static Task<string?> DetectScannerPortAsync(Action<string>? log = null) 
        => SerialPortHelpers.DetectScannerPortAsync(log);

    /// <summary>
    /// Gets all available serial port names without filtering.
    /// </summary>
    /// <remarks>Delegates to <see cref="SerialPortHelpers.GetAvailablePorts"/>.</remarks>
    public static string[] GetAvailablePorts() => SerialPortHelpers.GetAvailablePorts();
}