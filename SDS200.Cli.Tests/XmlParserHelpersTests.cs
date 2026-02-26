namespace SdsRemote.Tests;

using Xunit;
using SDS200.Cli.Logic;

public class XmlParserHelpersTests
{
    // ── Attr ──────────────────────────────────────────────────────────────

    [Fact]
    public void Attr_ReturnsAttributeValue_WhenPresent()
    {
        var el = new System.Xml.Linq.XElement("Test",
            new System.Xml.Linq.XAttribute("Name", "Alpha"));

        Assert.Equal("Alpha", XmlParserHelpers.Attr(el, "Name"));
    }

    [Fact]
    public void Attr_ReturnsFallback_WhenAttributeMissing()
    {
        var el = new System.Xml.Linq.XElement("Test");
        Assert.Equal("---", XmlParserHelpers.Attr(el, "Missing"));
    }

    [Fact]
    public void Attr_ReturnsFallback_WhenAttributeEmpty()
    {
        var el = new System.Xml.Linq.XElement("Test",
            new System.Xml.Linq.XAttribute("Name", ""));
        Assert.Equal("---", XmlParserHelpers.Attr(el, "Name"));
    }

    [Fact]
    public void Attr_ReturnsCustomFallback()
    {
        var el = new System.Xml.Linq.XElement("Test");
        Assert.Equal("SCANNING", XmlParserHelpers.Attr(el, "Missing", "SCANNING"));
    }

    // ── ParseFrequency ─────────────────────────────────────────────────

    [Theory]
    [InlineData("154.4150MHz", 154.4150)]
    [InlineData("863.5625MHz", 863.5625)]
    [InlineData("154.2800MHz", 154.2800)]
    [InlineData("25.0000MHz", 25.0000)]
    [InlineData("6000.0000MHz", 6000.0000)]
    public void ParseFrequency_ParsesValidStrings(string raw, double expected)
    {
        var result = XmlParserHelpers.ParseFrequency(raw);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Value, precision: 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("---")]
    [InlineData(null)]
    public void ParseFrequency_ReturnsNull_ForInvalidInput(string? raw)
    {
        Assert.Null(XmlParserHelpers.ParseFrequency(raw!));
    }

    // ── ExtractXmlFromGsiResponse ──────────────────────────────────────

    [Fact]
    public void ExtractXmlFromGsiResponse_StripsPrefixEnvelope()
    {
        string raw = "GSI,<XML>,<?xml version=\"1.0\"?><ScannerInfo/>";
        var result = XmlParserHelpers.ExtractXmlFromGsiResponse(raw);
        Assert.NotNull(result);
        Assert.StartsWith("<?xml", result);
    }

    [Fact]
    public void ExtractXmlFromGsiResponse_ReturnsRawXml_WhenNoEnvelope()
    {
        string raw = "<?xml version=\"1.0\"?><ScannerInfo/>";
        var result = XmlParserHelpers.ExtractXmlFromGsiResponse(raw);
        Assert.NotNull(result);
        Assert.StartsWith("<?xml", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ExtractXmlFromGsiResponse_ReturnsNull_ForEmpty(string? raw)
    {
        Assert.Null(XmlParserHelpers.ExtractXmlFromGsiResponse(raw!));
    }

    [Fact]
    public void ExtractXmlFromGsiResponse_ReturnsNull_WhenNoXmlStart()
    {
        Assert.Null(XmlParserHelpers.ExtractXmlFromGsiResponse("MDL,SDS200,1.00.00"));
    }
}

