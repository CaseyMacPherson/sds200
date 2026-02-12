namespace SdsRemote.Models;

public class ScannerStatus
{
    public double Frequency { get; set; }
    public string Modulation { get; set; } = "---";
    public string SystemName { get; set; } = "SCANNING";
    public string DepartmentName { get; set; } = "...";
    public string ChannelName { get; set; } = "...";
    public string Rssi { get; set; } = "S0";
    public string LastCommandSent { get; set; } = "None";
}