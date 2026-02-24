namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// Abstraction for scanner communication bridges.
/// Implementations provide Serial (USB) or UDP (Network) transport.
/// </summary>
public interface IScannerBridge : IDisposable
{
    /// <summary>Gets the current connection status.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the scanner via Serial or UDP.
    /// </summary>
    /// <param name="target">Serial port name (e.g., "/dev/cu.usbserial-12345") or IP address.</param>
    /// <param name="portOrBaud">Baud rate for serial, or UDP port number.</param>
    Task ConnectAsync(string target, int portOrBaud);

    /// <summary>
    /// Sends a command and waits for a response with timeout protection.
    /// Returns "TIMEOUT" if no response received within the timeout period.
    /// Returns "DISCONNECTED" if the bridge is not connected.
    /// </summary>
    /// <param name="command">Command string (e.g., "GSI,0", "MDL").</param>
    /// <param name="timeout">Maximum time to wait for response.</param>
    /// <returns>Response string, "TIMEOUT", or "DISCONNECTED".</returns>
    Task<string> SendAndReceiveAsync(string command, TimeSpan timeout);

    /// <summary>
    /// Sends a command without waiting for a response.
    /// Used for fire-and-forget commands like "MUT,ON" or "REC,OFF".
    /// </summary>
    /// <param name="command">Command string to send.</param>
    Task SendCommandAsync(string command);

    /// <summary>Fired when data is received from the scanner.</summary>
    event Action<string>? OnDataReceived;

    /// <summary>Fired when data is sent to the scanner.</summary>
    event Action<string>? OnDataSent;
}

