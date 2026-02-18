namespace SDS200.Cli.Core;

/// <summary>
/// Abstraction for protocol-specific data receiving behavior.
/// Serial and UDP protocols have different buffering requirements:
/// - Serial: Stream-based, buffers until \r delimiter
/// - UDP: Packet-based, may require multi-packet assembly for XML responses
/// </summary>
public interface IDataReceiver
{
    /// <summary>
    /// Event fired when a complete message/response line is received.
    /// For simple commands, this is a single line.
    /// For XML commands, this may be assembled from multiple packets.
    /// </summary>
    event Action<string>? OnMessageReceived;

    /// <summary>
    /// Starts the receiver loop.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registers a pending command that expects a response.
    /// Used to correlate responses with requests.
    /// </summary>
    void ExpectResponse(TaskCompletionSource<string> tcs, bool isXmlCommand);
}

