namespace SdsRemote.Core;

public interface IScannerBridge : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(string target, int portOrBaud);
    Task SendCommandAsync(string command);
    event Action<string>? OnDataReceived;
}