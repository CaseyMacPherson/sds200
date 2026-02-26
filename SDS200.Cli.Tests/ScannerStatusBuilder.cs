using SDS200.Cli.Abstractions.Models;

namespace SdsRemote.Tests;

/// <summary>
/// Fluent builder for constructing <see cref="ScannerStatus"/> test fixtures.
/// Provides sensible defaults so tests only need to override what matters.
/// </summary>
public class ScannerStatusBuilder
{
    private string _vscreen = "conventional_scan";
    private string _mode = "Scan";
    private string _systemName = "TEST_SYS";
    private string _department = "TEST_DEPT";
    private string _channelName = "TEST_CH";
    private double _frequency = 154.2800;
    private string _modulation = "FM";
    private string _tgId = "---";
    private string _siteName = "---";
    private string _unitId = "---";
    private string _rssi = "S0";
    private int _lastRssiValue = 0;
    private bool _signalLocked = false;
    private int _volume = 15;
    private int _squelch = 10;
    private string _hold = "Off";
    private string _recording = "Off";
    private string _mute = "Unmute";

    /// <summary>Sets the V_Screen mode string (e.g., "trunk_scan", "conventional_scan").</summary>
    public ScannerStatusBuilder WithVScreen(string vscreen) { _vscreen = vscreen; return this; }

    /// <summary>Sets the Mode attribute.</summary>
    public ScannerStatusBuilder WithMode(string mode) { _mode = mode; return this; }

    /// <summary>Sets the system name.</summary>
    public ScannerStatusBuilder WithSystem(string name) { _systemName = name; return this; }

    /// <summary>Sets the department name.</summary>
    public ScannerStatusBuilder WithDepartment(string name) { _department = name; return this; }

    /// <summary>Sets the channel name.</summary>
    public ScannerStatusBuilder WithChannel(string name) { _channelName = name; return this; }

    /// <summary>Sets the frequency in MHz.</summary>
    public ScannerStatusBuilder WithFrequency(double mhz) { _frequency = mhz; return this; }

    /// <summary>Sets the modulation string (e.g., "FM", "P25").</summary>
    public ScannerStatusBuilder WithModulation(string mod) { _modulation = mod; return this; }

    /// <summary>Sets the TGID (trunk scan).</summary>
    public ScannerStatusBuilder WithTgId(string tgid) { _tgId = tgid; return this; }

    /// <summary>Sets the site name (trunk scan).</summary>
    public ScannerStatusBuilder WithSite(string site) { _siteName = site; return this; }

    /// <summary>Sets the unit ID.</summary>
    public ScannerStatusBuilder WithUnitId(string uid) { _unitId = uid; return this; }

    /// <summary>Sets both the string RSSI (e.g., "S5") and the numeric value.</summary>
    public ScannerStatusBuilder WithRssi(int numericValue)
    {
        _rssi = $"S{numericValue}";
        _lastRssiValue = numericValue;
        return this;
    }

    /// <summary>Sets the signal locked state.</summary>
    public ScannerStatusBuilder WithSignalLocked(bool locked) { _signalLocked = locked; return this; }

    /// <summary>Sets volume and squelch levels.</summary>
    public ScannerStatusBuilder WithVolume(int vol, int sql = 10) { _volume = vol; _squelch = sql; return this; }

    /// <summary>Sets the Hold state ("On" / "Off").</summary>
    public ScannerStatusBuilder WithHold(string hold) { _hold = hold; return this; }

    /// <summary>Sets the recording state ("On" / "Off").</summary>
    public ScannerStatusBuilder WithRecording(string recording) { _recording = recording; return this; }

    /// <summary>Sets the mute state ("Mute" / "Unmute").</summary>
    public ScannerStatusBuilder WithMute(string mute) { _mute = mute; return this; }

    /// <summary>Builds the configured <see cref="ScannerStatus"/> instance.</summary>
    public ScannerStatus Build() => new()
    {
        VScreen = _vscreen,
        Mode = _mode,
        SystemName = _systemName,
        DepartmentName = _department,
        ChannelName = _channelName,
        Frequency = _frequency,
        Modulation = _modulation,
        TgId = _tgId,
        SiteName = _siteName,
        UnitId = _unitId,
        Rssi = _rssi,
        LastRssiValue = _lastRssiValue,
        SignalLocked = _signalLocked,
        Volume = _volume,
        Squelch = _squelch,
        Hold = _hold,
        Recording = _recording,
        Mute = _mute
    };
}

