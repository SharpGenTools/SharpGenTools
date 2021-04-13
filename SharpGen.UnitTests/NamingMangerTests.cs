using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Transform;
using Xunit;

namespace SharpGen.UnitTests
{
    public class NamingMangerTests
    {
        [Fact]
        public void ShortNamingRules()
        {
            var manager = new NamingRulesManager();

            manager.AddShortNameRule("DESC", "Description");

            var parameters = new[]
            {
                new CppParameter("pShaderDESC"),
                new CppParameter("pShaderDESC") {Pointer = "*"},
                new CppParameter("pShader_DESC") {Pointer = "*"},
                new CppParameter("p0") {Pointer = "*"},
                new CppParameter("p0") {Pointer = "*"},
                new CppParameter("0"),
                new CppParameter("break"),
                new CppParameter("void"),
                new CppParameter("string")
            };

            Assert.Equal(
                new[]
                {
                    "pShaderDESC",
                    "shaderDESCRef",
                    "shaderDescriptionRef",
                    "p0",
                    "p01",
                    "arg0",
                    "@break",
                    "@void",
                    "text",
                },
                manager.Rename(parameters)
            );

            foreach (var cppParameter in parameters)
                cppParameter.Rule.NamingFlags = NamingFlags.NoHungarianNotationHandler;

            Assert.Equal(
                new[]
                {
                    "pShaderDESC",
                    "pShaderDESC1",
                    "pShaderDescription",
                    "p0",
                    "p01",
                    "arg0",
                    "@break",
                    "@void",
                    "text",
                },
                manager.Rename(parameters)
            );

            foreach (var cppParameter in parameters)
                cppParameter.Rule.NamingFlags = NamingFlags.NoShortNameExpand | NamingFlags.KeepUnderscore |
                                                NamingFlags.NoHungarianNotationHandler | NamingFlags.NoPrematureBreak;

            Assert.Equal(
                new[]
                {
                    "pShaderDESC",
                    "pShaderDESC1",
                    "pShader_Desc",
                    "p0",
                    "p01",
                    "arg0",
                    "@break",
                    "@void",
                    "text",
                },
                manager.Rename(parameters)
            );

            Assert.Equal("ShaderDESC", manager.Rename(new CppInterface("ShaderDESC")));
            Assert.Equal("ShaderDescription", manager.Rename(new CppInterface("Shader_DESC")));
            Assert.Equal("ShaderDescription", manager.Rename(new CppInterface("SHADER_DESC")));
            Assert.Equal("ShaderDescription", manager.Rename(new CppStruct("SHADER_DESC")));
            Assert.Equal("ShaderDescription", manager.Rename(new CppEnum("SHADER_DESC")));
        }

        [Fact]
        public void LongerRuleTakesPrecedent()
        {
            var manager = new NamingRulesManager();

            manager.AddShortNameRule("INFO", "Information");
            manager.AddShortNameRule("INFORMATION", "Information");

            Assert.Equal("RawDeviceInformation", manager.Rename(new CppStruct("RAW_DEVICE_INFO")));

            Assert.Equal("RawDeviceInformation", manager.Rename(new CppStruct("RAW_DEVICE_INFORMATION")));
        }

        [Fact]
        public void Destination()
        {
            var manager = new NamingRulesManager();

            manager.AddShortNameRule("DST", "Destination");
            manager.AddShortNameRule("DEST", "Destination");

            CppEnum cppEnum = new("COLORMANAGEMENT_PROP");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_SOURCE_COLOR_CONTEXT", "0");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_SOURCE_RENDERING_INTENT", "1");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_DESTINATION_COLOR_CONTEXT", "2");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_DESTINATION_RENDERING_INTENT", "3");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_ALPHA_MODE", "4");
            cppEnum.AddEnumItem("COLORMANAGEMENT_PROP_QUALITY", "5");

            Assert.Equal("ColormanagementProp", manager.Rename(cppEnum));

            Assert.Equal(
                new[]
                {
                    "SourceColorContext",
                    "SourceRenderingIntent",
                    "DestinationInationColorContext",
                    "DestinationInationRenderingIntent",
                    "AlphaMode",
                    "Quality",
                },
                RenameEnumItems()
            );

            foreach (var cppEnumItem in cppEnum.EnumItems)
                cppEnumItem.Rule.NamingFlags = NamingFlags.NoShortNameExpand;

            Assert.Equal(
                new[]
                {
                    "SourceColorContext",
                    "SourceRenderingIntent",
                    "DestinationColorContext",
                    "DestinationRenderingIntent",
                    "AlphaMode",
                    "Quality",
                },
                RenameEnumItems()
            );

            foreach (var cppEnumItem in cppEnum.EnumItems)
                cppEnumItem.Rule.NamingFlags = NamingFlags.NoShortNameExpand | NamingFlags.KeepUnderscore |
                                               NamingFlags.NoHungarianNotationHandler | NamingFlags.NoPrematureBreak;

            Assert.Equal(
                new[]
                {
                    "Source_Color_Context",
                    "Source_Rendering_Intent",
                    "Destination_Color_Context",
                    "Destination_Rendering_Intent",
                    "Alpha_Mode",
                    "Quality",
                },
                RenameEnumItems()
            );

            string[] RenameEnumItems() => cppEnum.EnumItems.Select(x => manager.Rename(x, cppEnum.Name)).ToArray();
        }
    }
}
