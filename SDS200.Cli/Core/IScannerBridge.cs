namespace SDS200.Cli.Core; // <--- This MUST match the 'using' in Program.cs

public interface IScannerBridge : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(string target, int portOrBaud);
    Task<string> SendAndReceiveAsync(string command, TimeSpan timeout); // The race-condition fix
    Task SendCommandAsync(string command);
    
    /// <summary>Fired when data is received from the scanner.</summary>
    event Action<string>? OnDataReceived;
    
    /// <summary>Fired when data is sent to the scanner.</summary>
    event Action<string>? OnDataSent;
}