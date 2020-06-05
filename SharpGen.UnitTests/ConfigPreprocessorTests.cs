using System.IO;
using System.Xml.Linq;
using SharpGen.Config;
using Xunit;

namespace SharpGen.UnitTests
{
    public class ConfigPreprocessorTests
    {
        [Fact]
        public void IfDefIncludesChildrenWhenMacroDefined()
        {
            using (var stringReader = new StringReader(
                Preprocessor.Preprocess(
                    $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Defined'><child /></pre:ifdef></root>",
                    "Defined")))
            {
                var document = XDocument.Load(stringReader);
                Assert.Single(document.Descendants("child"));
            }
        }

        [Fact]
        public void IfDefExcludesChildrenWhenMacroUndefined()
        {
            using (var stringReader = new StringReader(
                Preprocessor.Preprocess(
                    $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Undefined'><child /></pre:ifdef><child /></root>")))
            {
                var document = XDocument.Load(stringReader);
                Assert.Single(document.Descendants("child"));
            }
        }

        [Fact]
        public void IfDefSupportsOrOperator()
        {
            using (var stringReader = new StringReader(
                Preprocessor.Preprocess(
                    $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifdef name='Undefined|Defined'><child /></pre:ifdef></root>",
                    "Defined")))
            {
                var document = XDocument.Load(stringReader);
                Assert.Single(document.Descendants("child"));
            }
        }

        [Fact]
        public void IfNDefExcludesChildrenWhenMacroDefined()
        {
            using (var stringReader = new StringReader(
                Preprocessor.Preprocess(
                    $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifndef name='Defined'><child /></pre:ifndef></root>",
                    "Defined")))
            {
                var document = XDocument.Load(stringReader);
                Assert.Empty(document.Descendants("child"));
            }
        }

        [Fact]
        public void IfNDefIncludesChildrenWhenMacroUndefined()
        {
            using (var stringReader = new StringReader(
                Preprocessor.Preprocess(
                    $"<root xmlns:pre='{ConfigFile.XmlNamespace}'><pre:ifndef name='Undefined'><child /></pre:ifndef></root>"
                    )))
            {
                var document = XDocument.Load(stringReader);
                Assert.Single(document.Descendants("child"));
            }
        }
    }
}
