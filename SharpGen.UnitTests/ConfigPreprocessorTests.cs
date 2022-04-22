using System.IO;
using System.Xml;
using System.Xml.Linq;
using SharpGen.Config;
using Xunit;

namespace SharpGen.UnitTests;

public class ConfigPreprocessorTests
{
    [Fact]
    public void IfDefIncludesChildrenWhenMacroDefined()
    {
        var document = Preprocess(
            $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Defined'><child /></pre:ifdef></root>",
            "Defined"
        );
        Assert.Single(document.Descendants("child"));
    }

    private static XDocument Preprocess(string xml, params string[] macros)
    {
        using var ppReader = XmlReader.Create(new StringReader(xml));
        using var ppWriter = new MemoryStream();
        Preprocessor.Preprocess(ppReader, ppWriter, macros);
        ppWriter.Position = 0;
        using var stringReader = new StreamReader(ppWriter);
        return XDocument.Load(stringReader);
    }

    [Fact]
    public void IfDefExcludesChildrenWhenMacroUndefined()
    {
        var document = Preprocess(
            $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Undefined'><child /></pre:ifdef><child /></root>"
        );
        Assert.Single(document.Descendants("child"));
    }

    [Fact]
    public void IfDefSupportsOrOperator()
    {
        var document = Preprocess(
            $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Undefined|Defined'><child /></pre:ifdef></root>",
            "Defined"
        );
        Assert.Single(document.Descendants("child"));
    }

    [Fact]
    public void IfNDefExcludesChildrenWhenMacroDefined()
    {
        var document = Preprocess(
            $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifndef name='Defined'><child /></pre:ifndef></root>",
            "Defined"
        );
        Assert.Empty(document.Descendants("child"));
    }

    [Fact]
    public void IfNDefIncludesChildrenWhenMacroUndefined()
    {
        var document = Preprocess(
            $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifndef name='Undefined'><child /></pre:ifndef></root>"
        );
        Assert.Single(document.Descendants("child"));
    }
}