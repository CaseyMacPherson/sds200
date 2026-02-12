namespace SdsRemote.Core; // <--- This MUST match the 'using' in Program.cs

public interface IScannerBridge : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(string target, int portOrBaud);
    Task<string> SendAndReceiveAsync(string command, TimeSpan timeout); // The race-condition fix
    Task SendCommandAsync(string command);
    event Action<string>? OnDataReceived;
}