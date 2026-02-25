using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;

/// <summary>
/// Base class for scanner bridge implementations.
/// Provides common command normalization, event invocation, and template methods.
/// </summary>
public abstract class ScannerBridgeBase : IScannerBridge
{
    /// <summary>Gets the current connection status.</summary>
    public abstract bool IsConnected { get; protected set; }

    /// <summary>Fired when data is received from the scanner.</summary>
    public event Action<string>? OnDataReceived;

    /// <summary>Fired when data is sent to the scanner.</summary>
    public event Action<string>? OnDataSent;

    /// <summary>
    /// Connects to the scanner via the transport-specific implementation.
    /// </summary>
    /// <param name="target">Serial port name or IP address.</param>
    /// <param name="portOrBaud">Baud rate for serial, or UDP port number.</param>
    public abstract Task ConnectAsync(string target, int portOrBaud);

    /// <summary>
    /// Sends a command and waits for a response with timeout protection.
    /// Template method that handles command normalization and delegates to transport-specific implementation.
    /// </summary>
    /// <param name="command">Command string (e.g., "GSI,0", "MDL").</param>
    /// <param name="timeout">Maximum time to wait for response.</param>
    /// <returns>Response string, "TIMEOUT", or "DISCONNECTED".</returns>
    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        var normalizedCommand = NormalizeCommand(command);
        RaiseDataSent(normalizedCommand);
        return await SendAndReceiveCoreAsync(normalizedCommand, timeout);
    }

    /// <summary>
    /// Sends a command without waiting for a response.
    /// Template method that handles command normalization and delegates to transport-specific implementation.
    /// </summary>
    /// <param name="command">Command string to send.</param>
    public async Task SendCommandAsync(string command)
    {
        var normalizedCommand = NormalizeCommand(command);
        RaiseDataSent(normalizedCommand);
        await SendCommandCoreAsync(normalizedCommand);
    }

    /// <summary>
    /// Transport-specific implementation for sending a command and receiving a response.
    /// </summary>
    /// <param name="normalizedCommand">The normalized command (uppercase, trimmed).</param>
    /// <param name="timeout">Maximum time to wait for response.</param>
    /// <returns>Response string or "TIMEOUT".</returns>
    protected abstract Task<string> SendAndReceiveCoreAsync(string normalizedCommand, TimeSpan timeout);

    /// <summary>
    /// Transport-specific implementation for fire-and-forget commands.
    /// </summary>
    /// <param name="normalizedCommand">The normalized command (uppercase, trimmed).</param>
    protected abstract Task SendCommandCoreAsync(string normalizedCommand);

    /// <summary>
    /// Normalizes a command string per protocol requirements.
    /// Commands must be uppercase and trimmed.
    /// </summary>
    /// <param name="command">The raw command string.</param>
    /// <returns>Normalized command string.</returns>
    protected static string NormalizeCommand(string command)
    {
        return command.ToUpper().Trim();
    }

    /// <summary>
    /// Raises the OnDataReceived event.
    /// </summary>
    /// <param name="data">The received data.</param>
    protected void RaiseDataReceived(string data)
    {
        OnDataReceived?.Invoke(data);
    }

    /// <summary>
    /// Raises the OnDataSent event.
    /// </summary>
    /// <param name="data">The sent data.</param>
    protected void RaiseDataSent(string data)
    {
        OnDataSent?.Invoke(data);
    }

    /// <summary>
    /// Disposes of transport-specific resources.
    /// </summary>
    public abstract void Dispose();
}

