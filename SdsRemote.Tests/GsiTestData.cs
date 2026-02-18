namespace SdsRemote.Tests;

/// <summary>
/// Test fixture data: sample XML responses from scanner GSI command.
/// </summary>
public static class GsiTestData
{
    /// <summary>Minimal valid GSI response with conventional scan.</summary>
    public static string ConventionalScanXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Scan" V_Screen="conventional_scan">
  <MonitorList Name="MyList" Index="1" />
  <System Name="FDNY" Index="1" />
  <Department Name="Fire" Index="1" />
  <ConvFrequency Name="Dispatch" Freq="154.2800MHz" Mod="FM" />
  <Property Rssi="3" VOL="15" SQL="10" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Trunk scan mode with TGID.</summary>
    public static string TrunkScanXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Scan" V_Screen="trunk_scan">
  <System Name="Police RAN" Index="1" />
  <Department Name="Dispatch" Index="1" />
  <Site Name="Central" Mod="P25" />
  <TGID Name="Traffic" TGID="1234" U_Id="5678" />
  <SiteFrequency Freq="863.5625MHz" />
  <Property Rssi="5" VOL="20" SQL="12" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Tone-out mode.</summary>
    public static string ToneOutXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="ToneOut" V_Screen="tone_out">
  <ToneOutChannel Name="Alert" Freq="146.5200MHz" Mod="FM" ToneA="100.0" ToneB="110.0" />
  <Property Rssi="2" VOL="18" SQL="11" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Quick search mode.</summary>
    public static string QuickSearchXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Search" V_Screen="quick_search">
  <SrchFrequency Freq="155.1600MHz" Mod="FM" />
  <SearchRange Lower="150.0000MHz" Upper="160.0000MHz" Mod="FM" />
  <Property Rssi="1" VOL="15" SQL="8" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Discovery conventional mode.</summary>
    public static string DiscoveryConventionalXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Discovery" V_Screen="discovery_conventional">
  <ConventionalDiscovery Freq="151.8950MHz" Mod="FM" Lower="150.0000MHz" Upper="160.0000MHz" HitCount="5" />
  <Property Rssi="4" VOL="17" SQL="9" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Discovery trunking mode.</summary>
    public static string DiscoveryTrunkingXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Discovery" V_Screen="discovery_trunking">
  <TrunkingDiscovery SystemName="Rail System" SiteName="Dispatch" TGID="5001" TgidName="Dispatchers" HitCount="12" />
  <Property Rssi="3" VOL="16" SQL="10" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Malformed TGID (should be ignored).</summary>
    public static string TrunkScanMalformedTgidXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Scan" V_Screen="trunk_scan">
  <System Name="Police" Index="1" />
  <Department Name="Dispatch" Index="1" />
  <Site Name="Main" />
  <TGID Name="Group1" TGID="TGID" />
  <SiteFrequency Freq="800.0000MHz" />
  <Property Rssi="0" VOL="15" SQL="10" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Zero RSSI (no signal).</summary>
    public static string NoSignalXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Scan" V_Screen="conventional_scan">
  <System Name="Silent" Index="1" />
  <Department Name="Waiting" Index="1" />
  <ConvFrequency Name="Idle" Freq="145.5500MHz" Mod="FM" />
  <Property Rssi="0" VOL="15" SQL="10" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Weather alert mode.</summary>
    public static string WeatherXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Weather" V_Screen="wx_alert">
  <WxChannel Name="WX1" Freq="162.5500MHz" Mod="FM" />
  <Property Rssi="3" VOL="20" SQL="12" Mute="Unmute" Att="Off" Rec="Off" />
</ScannerInfo>
""";

    /// <summary>Invalid XML (should fail gracefully).</summary>
    public static string InvalidXml => "GSI,<XML>,<ScannerInfo BROKEN";

    /// <summary>Empty/missing elements.</summary>
    public static string MinimalXml => """
GSI,<XML>,<?xml version="1.0" encoding="utf-8"?>
<ScannerInfo Mode="Test" V_Screen="unknown">
  <Property Rssi="1" />
</ScannerInfo>
""";
}
