namespace SdsRemote.Logic;
using SdsRemote.Models;

public static class UnidenParser
{
public static void UpdateStatus(ScannerStatus status, string rawData)
{
    // The SDS200 returns GSI,0 followed by data
    if (!rawData.StartsWith("GSI,0")) return;

    var p = rawData.Split(',');
    if (p.Length < 30) return; // Safety check for malformed strings

    // Index 2: System Name
    // Index 3: Department Name
    // Index 4: Channel Name
    // Index 5: Frequency (10Hz units - e.g., 04601250 = 460.125 MHz)
    // Index 6: Modulation (AM, NFM, FMN, etc.)
    // Index 10: RSSI (Signal strength bars 0-5)

    status.SystemName = p[2].Trim();
    status.DepartmentName = p[3].Trim();
    status.ChannelName = p[4].Trim();

    if (double.TryParse(p[5], out double rawFreq))
        status.Frequency = rawFreq / 100000.0; // Correct divisor for 10Hz units to MHz

    status.Modulation = p[6];
    status.Rssi = $"S{p[10]}"; // Prepends 'S' for our UI meter
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