using SDS200.Cli.Models;

namespace SDS200.Cli.Logic;
using Cli.Models;
using System.Xml.Linq;
using System.Text.RegularExpressions;

public static class UnidenParser
{
    public static bool UpdateStatus(ScannerStatus status, string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData)) return false;

        try
        {
            string? xmlContent = ExtractXmlFromGsiResponse(rawData);
            if (string.IsNullOrEmpty(xmlContent)) return false;

            if (!xmlContent.Contains("</ScannerInfo>")) return false;

            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;
            if (root?.Name.LocalName != "ScannerInfo") return false;

            // Mode & V_Screen attributes on the root element
            status.Mode = root.Attribute("Mode")?.Value ?? "---";
            status.VScreen = root.Attribute("V_Screen")?.Value ?? "---";

            // Dispatch based on V_Screen to extract mode-specific child elements
            switch (status.VScreen)
            {
                case "conventional_scan":
                    ParseConventionalScan(root, status);
                    break;
                case "trunk_scan":
                    ParseTrunkScan(root, status);
                    break;
                case "custom_with_scan":
                    ParseCustomWithScan(root, status);
                    break;
                case "cchits_with_scan":
                    ParseCcHitsScan(root, status);
                    break;
                case "custom_search":
                    ParseCustomSearch(root, status);
                    break;
                case "quick_search":
                    ParseQuickSearch(root, status);
                    break;
                case "close_call":
                case "cc_searching":
                    ParseCloseCall(root, status);
                    break;
                case "tone_out":
                    ParseToneOut(root, status);
                    break;
                case "wx_alert":
                    ParseWeather(root, status);
                    break;
                case "repeater_find":
                case "reverse_frequency":
                case "direct_entry":
                    ParseRepeaterFind(root, status);
                    break;
                case "discovery_conventional":
                    ParseDiscoveryConventional(root, status);
                    break;
                case "discovery_trunking":
                    ParseDiscoveryTrunking(root, status);
                    break;
                case "analyze_system_status":
                    ParseAnalyzeSystemStatus(root, status);
                    break;
                case "analyze":
                    ParseWaterfall(root, status);
                    break;
                default:
                    // Unknown V_Screen — try conventional as fallback
                    ParseConventionalScan(root, status);
                    break;
            }

            // Common elements (always present regardless of mode)
            ParseProperty(root, status);
            ParseMonitorList(root, status);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Mode-specific parsers ──────────────────────────────────────────

    /// <summary>conventional_scan: MonitorList, System, Department, ConvFrequency, DualWatch</summary>
    private static void ParseConventionalScan(XElement root, ScannerStatus s)
    {
        ParseSystem(root, s);
        ParseDepartment(root, s);
        var freq = root.Element("ConvFrequency");
        if (freq != null)
        {
            s.ChannelName = Attr(freq, "Name");
            s.Modulation = Attr(freq, "Mod");
            s.ServiceType = Attr(freq, "SvcType");
            s.Hold = Attr(freq, "Hold", "Off");
            SetFrequency(s, Attr(freq, "Freq"));
        }
    }

    /// <summary>trunk_scan: MonitorList, System, Department, Site, TGID, SiteFrequency, DualWatch</summary>
    private static void ParseTrunkScan(XElement root, ScannerStatus s)
    {
        ParseSystem(root, s);
        ParseDepartment(root, s);

        var site = root.Element("Site");
        if (site != null)
        {
            s.SiteName = Attr(site, "Name");
            s.Modulation = Attr(site, "Mod");
        }

        var tgid = root.Element("TGID");
        if (tgid != null)
        {
            s.ChannelName = Attr(tgid, "Name");
            s.TgId = Attr(tgid, "TGID");
            s.UnitId = Attr(tgid, "U_Id");
            s.ServiceType = Attr(tgid, "SvcType");
            s.Hold = Attr(tgid, "Hold", "Off");
        }

        var siteFreq = root.Element("SiteFrequency");
        if (siteFreq != null)
            SetFrequency(s, Attr(siteFreq, "Freq"));
    }

    /// <summary>custom_with_scan: MonitorList, System, Department, SrchFrequency, DualWatch, SearchRange, SearchBanks</summary>
    private static void ParseCustomWithScan(XElement root, ScannerStatus s)
    {
        ParseSystem(root, s);
        ParseDepartment(root, s);
        ParseSrchFrequency(root, s);
        ParseSearchRange(root, s);
    }

    /// <summary>cchits_with_scan: MonitorList, System, Department, CcHitsChannel, DualWatch</summary>
    private static void ParseCcHitsScan(XElement root, ScannerStatus s)
    {
        ParseSystem(root, s);
        ParseDepartment(root, s);

        var ch = root.Element("CcHitsChannel");
        if (ch != null)
        {
            s.ChannelName = Attr(ch, "Name");
            s.Modulation = Attr(ch, "Mod");
            s.Hold = Attr(ch, "Hold", "Off");
            SetFrequency(s, Attr(ch, "Freq"));
        }
    }

    /// <summary>custom_search: SrchFrequency, DualWatch, SearchRange, SearchBanks</summary>
    private static void ParseCustomSearch(XElement root, ScannerStatus s)
    {
        ParseSrchFrequency(root, s);
        ParseSearchRange(root, s);
    }

    /// <summary>quick_search: SrchFrequency, DualWatch, SearchRange</summary>
    private static void ParseQuickSearch(XElement root, ScannerStatus s)
    {
        ParseSrchFrequency(root, s);
        ParseSearchRange(root, s);
    }

    /// <summary>close_call / cc_searching: SrchFrequency, DualWatch, SearchRange, CC_Bands</summary>
    private static void ParseCloseCall(XElement root, ScannerStatus s)
    {
        ParseSrchFrequency(root, s);
        ParseSearchRange(root, s);
    }

    /// <summary>tone_out: ToneOutChannel</summary>
    private static void ParseToneOut(XElement root, ScannerStatus s)
    {
        var ch = root.Element("ToneOutChannel");
        if (ch != null)
        {
            s.ChannelName = Attr(ch, "Name");
            s.Modulation = Attr(ch, "Mod");
            s.Hold = Attr(ch, "Hold", "Off");
            s.ToneA = Attr(ch, "ToneA");
            s.ToneB = Attr(ch, "ToneB");
            SetFrequency(s, Attr(ch, "Freq"));
        }
    }

    /// <summary>wx_alert: WxChannel, WxMode, DualWatch</summary>
    private static void ParseWeather(XElement root, ScannerStatus s)
    {
        var ch = root.Element("WxChannel");
        if (ch != null)
        {
            s.ChannelName = Attr(ch, "Name");
            s.Modulation = Attr(ch, "Mod");
            s.Hold = Attr(ch, "Hold", "Off");
            SetFrequency(s, Attr(ch, "Freq"));
        }
    }

    /// <summary>repeater_find / reverse_frequency / direct_entry: SrchFrequency, DualWatch</summary>
    private static void ParseRepeaterFind(XElement root, ScannerStatus s)
    {
        ParseSrchFrequency(root, s);
    }

    /// <summary>discovery_conventional: ConventionalDiscovery</summary>
    private static void ParseDiscoveryConventional(XElement root, ScannerStatus s)
    {
        var disc = root.Element("ConventionalDiscovery");
        if (disc != null)
        {
            s.Modulation = Attr(disc, "Mod");
            s.SearchRangeLower = Attr(disc, "Lower");
            s.SearchRangeUpper = Attr(disc, "Upper");
            s.TgId = Attr(disc, "TGID");
            s.UnitId = Attr(disc, "U_Id");
            SetFrequency(s, Attr(disc, "Freq"));
            if (int.TryParse(disc.Attribute("HitCount")?.Value, out int hits))
                s.HitCount = hits;
        }
    }

    /// <summary>discovery_trunking: TrunkingDiscovery</summary>
    private static void ParseDiscoveryTrunking(XElement root, ScannerStatus s)
    {
        var disc = root.Element("TrunkingDiscovery");
        if (disc != null)
        {
            s.SystemName = Attr(disc, "SystemName", "SCANNING");
            s.SiteName = Attr(disc, "SiteName");
            s.TgId = Attr(disc, "TGID");
            s.ChannelName = Attr(disc, "TgidName");
            s.UnitId = Attr(disc, "U_Id");
            if (int.TryParse(disc.Attribute("HitCount")?.Value, out int hits))
                s.HitCount = hits;
        }
    }

    /// <summary>analyze_system_status: SystemStatus</summary>
    private static void ParseAnalyzeSystemStatus(XElement root, ScannerStatus s)
    {
        var ss = root.Element("SystemStatus");
        if (ss != null)
        {
            s.SystemName = Attr(ss, "SystemName", "SCANNING");
            s.SiteName = Attr(ss, "SiteName");
            s.Attenuator = Attr(ss, "Att", "Off");
            s.P25Status = Attr(ss, "P25Status");
        }
    }

    /// <summary>analyze (waterfall): Analyze, WaterfallBand, WaterfallSettings</summary>
    private static void ParseWaterfall(XElement root, ScannerStatus s)
    {
        var analyze = root.Element("Analyze");
        if (analyze != null)
        {
            s.SystemName = Attr(analyze, "SystemName", "SCANNING");
            s.SiteName = Attr(analyze, "SiteName");
            s.ChannelName = Attr(analyze, "Msg1");
        }

        var band = root.Element("WaterfallBand");
        if (band != null)
        {
            s.SearchRangeLower = Attr(band, "Lower");
            s.SearchRangeUpper = Attr(band, "Upper");
            s.Modulation = Attr(band, "Mod");
            SetFrequency(s, Attr(band, "Center"));
        }
    }

    // ── Shared element parsers ─────────────────────────────────────────

    private static void ParseMonitorList(XElement root, ScannerStatus s)
    {
        var ml = root.Element("MonitorList");
        if (ml != null)
            s.MonitorListName = Attr(ml, "Name");
    }

    private static void ParseSystem(XElement root, ScannerStatus s)
    {
        var sys = root.Element("System");
        if (sys != null)
            s.SystemName = Attr(sys, "Name", "SCANNING");
    }

    private static void ParseDepartment(XElement root, ScannerStatus s)
    {
        var dept = root.Element("Department");
        if (dept != null)
            s.DepartmentName = Attr(dept, "Name", "...");
    }

    private static void ParseSrchFrequency(XElement root, ScannerStatus s)
    {
        var freq = root.Element("SrchFrequency");
        if (freq != null)
        {
            s.Modulation = Attr(freq, "Mod");
            SetFrequency(s, Attr(freq, "Freq"));
        }
    }

    private static void ParseSearchRange(XElement root, ScannerStatus s)
    {
        var range = root.Element("SearchRange");
        if (range != null)
        {
            s.SearchRangeLower = Attr(range, "Lower");
            s.SearchRangeUpper = Attr(range, "Upper");
        }
    }

    private static void ParseProperty(XElement root, ScannerStatus s)
    {
        var prop = root.Element("Property");
        if (prop == null) return;

        // Extract RSSI both as string (S0, S5, etc.) and as numeric for threshold detection
        string rssiRaw = prop.Attribute("Rssi")?.Value ?? "0";
        s.Rssi = string.IsNullOrEmpty(rssiRaw) ? "S0" : $"S{rssiRaw}";
        
        // Parse numeric RSSI value for threshold detection
        if (int.TryParse(rssiRaw, out int rssiNum))
            s.LastRssiValue = rssiNum;
        
        s.Mute = Attr(prop, "Mute", "Unmute");
        s.Attenuator = Attr(prop, "Att", "Off");
        s.AlertLed = Attr(prop, "A_Led", "Off");
        s.P25Status = Attr(prop, "P25Status");
        s.Recording = Attr(prop, "Rec", "Off");

        if (int.TryParse(prop.Attribute("VOL")?.Value, out int vol))
            s.Volume = vol;
        if (int.TryParse(prop.Attribute("SQL")?.Value, out int sql))
            s.Squelch = sql;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>Read an attribute value, returning a fallback if missing or empty.</summary>
    private static string Attr(XElement el, string name, string fallback = "---")
        => el.Attribute(name)?.Value is { Length: > 0 } v ? v : fallback;

    /// <summary>Parse a frequency string like "154.4150MHz" into a double and assign it.</summary>
    private static void SetFrequency(ScannerStatus s, string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "---") return;
        string cleaned = Regex.Replace(raw, "[^0-9.]", "");
        if (double.TryParse(cleaned, out double freq))
            s.Frequency = freq;
    }

    /// <summary>
    /// Strips the "GSI,&lt;XML&gt;," envelope from a raw GSI response and returns clean XML.
    /// </summary>
    private static string? ExtractXmlFromGsiResponse(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData)) return null;

        string data = rawData.Trim();

        const string envelope = "GSI,<XML>,";
        int envelopeEnd = data.IndexOf(envelope);

        if (envelopeEnd >= 0)
        {
            int xmlStartIndex = envelopeEnd + envelope.Length;
            if (xmlStartIndex < data.Length)
                data = data.Substring(xmlStartIndex).Trim();
            else
                return null;
        }

        if (data.StartsWith("<"))
            return data;

        return null;
    }
}