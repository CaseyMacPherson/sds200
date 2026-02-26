using System.Text.RegularExpressions;

namespace SDS200.Cli.Logic;

/// <summary>
/// Pure static helper utilities for XML parsing operations.
/// Shared across parser implementations without carrying any state.
/// </summary>
public static class XmlParserHelpers
{
    /// <summary>
    /// Reads an XML attribute value, returning <paramref name="fallback"/> if the attribute
    /// is missing or empty.
    /// </summary>
    /// <param name="el">The XML element to read from.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="fallback">Value returned when the attribute is absent or empty.</param>
    public static string Attr(System.Xml.Linq.XElement el, string name, string fallback = "---")
        => el.Attribute(name)?.Value is { Length: > 0 } v ? v : fallback;

    /// <summary>
    /// Parses a frequency string such as <c>"154.4150MHz"</c> into a <see cref="double"/>.
    /// Returns <c>null</c> if the string cannot be parsed.
    /// </summary>
    /// <param name="raw">The raw frequency string from the scanner XML.</param>
    public static double? ParseFrequency(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "---") return null;
        string cleaned = Regex.Replace(raw, "[^0-9.]", "");
        return double.TryParse(cleaned, out double freq) ? freq : null;
    }

    /// <summary>
    /// Strips the <c>"GSI,&lt;XML&gt;,"</c> envelope from a raw GSI response and returns
    /// the inner XML string, or <c>null</c> if the data cannot be interpreted as XML.
    /// </summary>
    /// <param name="rawData">The full raw string received from the scanner.</param>
    public static string? ExtractXmlFromGsiResponse(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData)) return null;

        string data = rawData.Trim();

        const string envelope = "GSI,<XML>,";
        int envelopeEnd = data.IndexOf(envelope, StringComparison.Ordinal);

        if (envelopeEnd >= 0)
        {
            int xmlStartIndex = envelopeEnd + envelope.Length;
            if (xmlStartIndex < data.Length)
                data = data[xmlStartIndex..].Trim();
            else
                return null;
        }

        return data.StartsWith('<') ? data : null;
    }
}

