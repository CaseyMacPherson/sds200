using SDS200.Cli.Abstractions.Core;

namespace SDS200.Cli.Bridges;

/// <summary>
/// Production implementation of IScannerBridgeFactory.
/// Creates real UDP and Serial bridge instances.
/// </summary>
public class ScannerBridgeFactory : IScannerBridgeFactory
{
    /// <summary>
    /// Creates a new UDP scanner bridge.
    /// </summary>
    /// <returns>A new UdpScannerBridge instance.</returns>
    public IScannerBridge CreateUdpBridge() => new UdpScannerBridge();

    /// <summary>
    /// Creates a new Serial scanner bridge.
    /// </summary>
    /// <returns>A new SerialScannerBridge instance.</returns>
    public IScannerBridge CreateSerialBridge() => new SerialScannerBridge();
}

