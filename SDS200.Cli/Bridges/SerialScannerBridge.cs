using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;
using System.IO.Ports;

/// <summary>
/// Serial scanner bridge for SDS200 communication over USB.
/// Uses standard serial port settings (115200, 8N1).
/// Delegates all buffering and line-assembly to <see cref="SerialDataReceiver"/>
/// — matching the same receiver pattern used by <see cref="UdpScannerBridge"/>.
/// </summary>
public class SerialScannerBridge : ScannerBridgeBase
{
    private SerialPort? _port;
    private SerialDataReceiver? _dataReceiver;

    /// <summary>Gets whether the serial port is open.</summary>
    public override bool IsConnected
    {
        get => _port?.IsOpen ?? false;
        protected set { /* Read-only — derived from port state */ }
    }

    /// <inheritdoc/>
    public override async Task ConnectAsync(string name, int baud)
    {
        _port = new SerialPort(name, baud, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        _dataReceiver = new SerialDataReceiver(_port);
        _dataReceiver.OnMessageReceived += line => RaiseDataReceived(line);

        _port.Open();

        // Start the serial receiver; it wires up the DataReceived event internally
        await _dataReceiver.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// Enables event monitoring so that <see cref="ScannerBridgeBase.OnDataReceived"/>
    /// fires for every inbound line (not just responses to SendAndReceiveAsync).
    /// For Serial transport this is always active once the port is open —
    /// the method is kept for API parity with callers in <see cref="Logic.ConnectionSetupService"/>.
    /// </summary>
    public void EnableEventMonitoring() { /* no-op: SerialDataReceiver always fires events */ }

    /// <summary>Disables event monitoring (no-op on Serial — kept for API symmetry).</summary>
    public void DisableEventMonitoring() { }

    /// <inheritdoc/>
    protected override async Task<string> SendAndReceiveCoreAsync(string normalizedCommand, TimeSpan timeout)
    {
        if (!IsConnected || _dataReceiver == null) return "DISCONNECTED";

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dataReceiver.ExpectResponse(tcs, isXmlCommand: false);

        _port!.Write(normalizedCommand + "\r");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        return completed == tcs.Task ? await tcs.Task : "TIMEOUT";
    }

    /// <inheritdoc/>
    protected override async Task SendCommandCoreAsync(string normalizedCommand)
    {
        if (IsConnected)
            _port!.Write(normalizedCommand + "\r");

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override void Dispose() => _port?.Dispose();

    /// <summary>
    /// Returns available serial ports, filtered to exclude debug and Bluetooth ports.
    /// Prefers cu.* (macOS call-out) over tty.*, and prioritises usbserial/usbmodem.
    /// </summary>
    public static string[] GetFilteredPorts() => SerialPortHelpers.GetFilteredPorts();

    /// <summary>
    /// Auto-detects the SDS200 scanner by probing each filtered port with the MDL command.
    /// Returns the port name if found, or <c>null</c>.
    /// </summary>
    public static Task<string?> DetectScannerPortAsync(Action<string>? log = null)
        => SerialPortHelpers.DetectScannerPortAsync(log);

    /// <summary>Gets all available serial port names without filtering.</summary>
    public static string[] GetAvailablePorts() => SerialPortHelpers.GetAvailablePorts();
}