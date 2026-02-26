using System.Collections.Concurrent;
using Spectre.Console;
using SDS200.Cli.Bridges;
using SDS200.Cli.Abstractions.Core;
using SDS200.Cli.Abstractions.Models;
using SDS200.Cli.Logic;

namespace SDS200.Cli.Presentation;

/// <summary>
/// Handles connection setup, mode selection, and bridge creation.
/// Extracts startup logic from Program.cs for better testability and separation of concerns.
/// </summary>
public class ConnectionSetupService
{
    private readonly AppSettings _settings;
    private readonly bool _isDebugMode;
    private readonly IScannerBridgeFactory _bridgeFactory;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Creates a new ConnectionSetupService.
    /// </summary>
    /// <param name="settings">Application settings for storing connection preferences.</param>
    /// <param name="bridgeFactory">Factory for creating scanner bridges (optional, defaults to production factory).</param>
    /// <param name="timeProvider">Time provider for timestamps (optional, defaults to system time).</param>
    /// <param name="isDebugMode">Whether running in debug mode (skips interactive prompts).</param>
    public ConnectionSetupService(
        AppSettings settings,
        IScannerBridgeFactory? bridgeFactory = null,
        ITimeProvider? timeProvider = null,
        bool isDebugMode = false)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _bridgeFactory = bridgeFactory ?? new ScannerBridgeFactory();
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _isDebugMode = isDebugMode;
    }

    /// <summary>
    /// Result of connection setup containing the configured bridge and handlers.
    /// </summary>
    public class SetupResult
    {
        public required IScannerBridge Bridge { get; init; }
        public required GsiResponseHandler ResponseHandler { get; init; }
        public bool Success { get; init; }
    }

    /// <summary>
    /// Performs full connection setup including mode selection, prompts, and bridge creation.
    /// </summary>
    /// <param name="status">Scanner status to update with GSI responses.</param>
    /// <param name="debugLog">Debug log queue for parsing events.</param>
    /// <param name="rawRadioData">Thread-safe queue for raw radio data display.</param>
    /// <returns>SetupResult with configured bridge and handlers, or null if setup failed.</returns>
    public async Task<SetupResult?> SetupAsync(
        ScannerStatus status,
        Queue<string> debugLog,
        ConcurrentQueue<string> rawRadioData)
    {
        // Show banner
        AnsiConsole.Write(new FigletText("SDS200 CLI").Color(Color.Orange1));

        // Select connection mode
        string modeChoice = SelectConnectionMode();

        // Create response handler
        var responseHandler = new GsiResponseHandler(status, debugLog, rawRadioData);

        IScannerBridge bridge;
        if (modeChoice.StartsWith("UDP"))
        {
            bridge = await SetupUdpConnectionAsync(responseHandler);
        }
        else
        {
            var result = await SetupSerialConnectionAsync(responseHandler);
            if (result == null) return null;
            bridge = result;
        }

        _settings.Save();

        return new SetupResult
        {
            Bridge = bridge,
            ResponseHandler = responseHandler,
            Success = bridge.IsConnected
        };
    }

    private string SelectConnectionMode()
    {
        if (_isDebugMode)
        {
            var modeChoice = _settings.LastMode == "Serial" ? "Serial (USB)" : "UDP (Network)";
            AnsiConsole.MarkupLine(MarkupConstants.FormatDebugModeStartup(modeChoice));
            return modeChoice;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Connect via:")
                .AddChoices("UDP (Network)", "Serial (USB)"));
    }

    private async Task<IScannerBridge> SetupUdpConnectionAsync(GsiResponseHandler responseHandler)
    {
        var bridge = _bridgeFactory.CreateUdpBridge();

        if (!_isDebugMode)
        {
            _settings.LastIp = AnsiConsole.Ask<string>(
                MarkupConstants.FormatScannerIpPrompt(_settings.LastIp), 
                _settings.LastIp);
        }

        _settings.LastMode = "UDP";

        // Register event handlers BEFORE connecting
        bridge.OnDataSent += responseHandler.OnDataSent;
        bridge.OnDataReceived += responseHandler.OnDataReceived;

        AnsiConsole.MarkupLine(MarkupConstants.FormatUdpConnecting(_settings.LastIp));
        await bridge.ConnectAsync(_settings.LastIp, 50536);

        if (bridge.IsConnected)
            AnsiConsole.MarkupLine(MarkupConstants.UdpConnectedSuccess);
        else
            AnsiConsole.MarkupLine(MarkupConstants.UdpConnectedFailure);

        return bridge;
    }

    private async Task<IScannerBridge?> SetupSerialConnectionAsync(GsiResponseHandler responseHandler)
    {
        var bridge = _bridgeFactory.CreateSerialBridge();

        string? port;
        if (_isDebugMode)
        {
            port = _settings.LastComPort;
            AnsiConsole.MarkupLine(MarkupConstants.FormatSerialConnecting(port));
        }
        else
        {
            port = await DetectOrSelectPortAsync();
            if (port == null) return null;
        }

        _settings.LastComPort = port;
        _settings.LastMode = "Serial";

        // Register event handlers before connecting
        bridge.OnDataSent += responseHandler.OnDataSent;
        bridge.OnDataReceived += responseHandler.OnDataReceived;

        await bridge.ConnectAsync(port, _settings.LastBaudRate);
        
        // Enable event monitoring if the bridge supports it
        if (bridge is SerialScannerBridge serialBridge)
        {
            serialBridge.EnableEventMonitoring();
        }

        return bridge;
    }

    private async Task<string?> DetectOrSelectPortAsync()
    {
        AnsiConsole.MarkupLine(MarkupConstants.SearchingForScanner);
        var detectedPort = await SerialScannerBridge.DetectScannerPortAsync(
            msg => AnsiConsole.MarkupLine(MarkupConstants.FormatGreyMessage(msg)));

        if (detectedPort != null)
        {
            AnsiConsole.MarkupLine(MarkupConstants.FormatScannerDetected(detectedPort));
            return detectedPort;
        }

        // Fallback to manual selection
        AnsiConsole.MarkupLine(MarkupConstants.AutoDetectFailed);
        var ports = SerialScannerBridge.GetFilteredPorts();

        if (ports.Length == 0)
        {
            AnsiConsole.MarkupLine(MarkupConstants.NoSerialPortsFound);
            return null;
        }

        var portPrompt = new SelectionPrompt<string>()
            .Title("Select Serial Port:")
            .AddChoices(ports);

        return AnsiConsole.Prompt(portPrompt);
    }
}

