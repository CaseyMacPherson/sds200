namespace SdsRemote.Logic;
using SdsRemote.Models;

public static class UnidenParser
{
    public static void UpdateStatus(ScannerStatus status, string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData)) return;
        var parts = rawData.Split(',');

        if (parts.Length >= 19 && parts[0] == "STS")
        {
            if (double.TryParse(parts[3], out double freq)) status.Frequency = freq / 1000000.0;
            status.Modulation = parts[10];
            status.SystemName = parts[15].Trim();
            status.DepartmentName = parts[16].Trim();
            status.ChannelName = parts[17].Trim();
            status.Rssi = parts[18];
        }
    }

    public static bool TryValidateFrequency(string input, out string formatted)
    {
        if (double.TryParse(input, out double freq) && freq >= 25.0 && freq <= 1300.0) {
            formatted = freq.ToString("F4");
            return true;
        }
        formatted = "";
        return false;
    }
}