namespace SDS200.Cli.Abstractions.Core;

/// <summary>
/// Factory for creating scanner bridge instances.
/// Allows dependency injection of bridge creation for testability.
/// </summary>
public interface IScannerBridgeFactory
{
    /// <summary>
    /// Creates a UDP scanner bridge.
    /// </summary>
    /// <returns>A new UDP scanner bridge instance.</returns>
    IScannerBridge CreateUdpBridge();

    /// <summary>
    /// Creates a Serial scanner bridge.
    /// </summary>
    /// <returns>A new Serial scanner bridge instance.</returns>
    IScannerBridge CreateSerialBridge();
}

