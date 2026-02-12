using System;
using System.Threading.Tasks;
using Spectre.Console;
using SdsRemote.Core;
using SdsRemote.Bridges;
using SdsRemote.Models;
using SdsRemote.Logic;

Queue<string> debugLog = new Queue<string>();
var settings = AppSettings.Load();
var status = new ScannerStatus();
IScannerBridge bridge;

AnsiConsole.Write(new FigletText("SDS200 CLI").Color(Color.Orange1));
// 1. Connection Logic
// We removed .DefaultValue() and replaced it with a simple Choice logic 
// that is compatible with all recent Spectre versions.
var modeChoice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Connect via:")
        .AddChoices("UDP (Network)", "Serial (USB)"));

if (modeChoice.StartsWith("UDP")) {
    bridge = new UdpScannerBridge();
    
    // For TextPrompt (Ask), .DefaultValue() works perfectly
    settings.LastIp = AnsiConsole.Ask<string>($"Scanner IP [grey]({settings.LastIp})[/]:", settings.LastIp);
    
    settings.LastMode = "UDP";
    await bridge.ConnectAsync(settings.LastIp, 50536);
} else {
    var serialBridge = new SerialScannerBridge();
    bridge = serialBridge;

    // Auto-detect the scanner port (filtered, validated with MDL command)
    AnsiConsole.MarkupLine("[yellow]Searching for SDS200 scanner...[/]");
    var detectedPort = await SerialScannerBridge.DetectScannerPortAsync(
        msg => AnsiConsole.MarkupLine($"[grey]{Markup.Escape(msg)}[/]"));

    string port;
    if (detectedPort != null)
    {
        AnsiConsole.MarkupLine($"[green]Scanner detected on {Markup.Escape(detectedPort)}[/]");
        port = detectedPort;
    }
    else
    {
        // Fallback to manual selection with filtered ports
        AnsiConsole.MarkupLine("[yellow]Auto-detect failed. Falling back to manual selection.[/]");
        var ports = SerialScannerBridge.GetFilteredPorts();

        if (ports.Length == 0) {
            AnsiConsole.MarkupLine("[red]No serial ports found! Check your hardware connection.[/]");
            return;
        }

        var portPrompt = new SelectionPrompt<string>()
            .Title("Select Serial Port:")
            .AddChoices(ports);

        port = AnsiConsole.Prompt(portPrompt);
    }

    settings.LastComPort = port;
    settings.LastMode = "Serial";
    await bridge.ConnectAsync(port, settings.LastBaudRate);

    // Enable event monitoring now that connection is established (avoids race condition)
    serialBridge.EnableEventMonitoring();
}

settings.Save();
bridge.OnDataReceived += (data) => {
    // Escape the entire entry so brackets [ ] don't crash the Spectre UI
    var safeData = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] {data.Trim()}");
    debugLog.Enqueue(safeData);
    
    if (debugLog.Count > 5) debugLog.Dequeue();
    UnidenParser.UpdateStatus(status, data);
};

await AnsiConsole.Live(Render(status, bridge.IsConnected, debugLog)).StartAsync(async ctx => {
    while (true)
    {
        // Update the TUI
        ctx.UpdateTarget(Render(status, bridge.IsConnected, debugLog));

        // Request the data and WAIT for the radio to finish its thought
        // This is the "Stop-and-Wait" flow the SDS200 requires
        string response = await bridge.SendAndReceiveAsync("GSI,0", TimeSpan.FromMilliseconds(500));

        if (response != "TIMEOUT" && response.StartsWith("GSI"))
        {
            UnidenParser.UpdateStatus(status, response);
        }
        else if (response == "TIMEOUT")
        {
            status.LastCommandSent = "⚠️ Radio Timeout - Retrying...";
        }

        // Give the CPU and the Radio a 100ms breather
        await Task.Delay(100);

        // Keyboard handling...
    }
});

static Layout Render(ScannerStatus s, bool conn, Queue<string> logs) {
    var info = new Table().NoBorder().HideHeaders().AddColumns("L", "V")
        .AddRow("[blue]SYS:[/]", s.SystemName)
        .AddRow("[blue]DEP:[/]", s.DepartmentName)
        .AddRow("[yellow]CH :[/]", $"[bold white]{s.ChannelName}[/]");

    var debugTable = new Table().NoBorder().HideHeaders().AddColumn("Log");
    foreach(var log in logs) {
        // Wrap the string in a Markup object to ensure it's handled correctly
        debugTable.AddRow(new Markup($"[grey]{log}[/]"));
    }

    var layout = new Layout("Root")
        .SplitRows(
            new Layout("Hero"),
            new Layout("Mid").SplitColumns(new Layout("Data"), new Layout("RSSI")),
            new Layout("Debug").Size(7), // New Debug Section
            new Layout("Footer").Size(3)
        );


    // Removed 'size=26' - using [bold green] for emphasis instead
    layout["Hero"].Update(new Panel(
        Align.Center(
            new Markup($"[bold green]{s.Frequency:F4}[/] [grey]MHz[/]"), 
            VerticalAlignment.Middle))
        .Border(BoxBorder.Double));

    layout["Data"].Update(new Panel(info).Header("Identity").Expand());
    
    layout["RSSI"].Update(new Panel(
        Align.Center(
            new Markup($"[bold yellow]{s.Rssi}[/]"), 
            VerticalAlignment.Middle))
        .Header("Signal"));

    layout["Footer"].Update(new Panel(
        new Text(conn ? "CONNECTED" : "DISCONNECTED", 
        new Style(conn ? Color.Green : Color.Red)))
        .Border(BoxBorder.None));
layout["Debug"].Update(new Panel(debugTable).Header("Raw Radio Traffic"));

    return layout;
}