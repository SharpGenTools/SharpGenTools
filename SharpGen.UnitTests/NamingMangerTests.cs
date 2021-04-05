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
            var paramNameList = manager.Rename(
                new[]
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
                }
            );
            Assert.Equal(9, paramNameList.Count);
            Assert.Equal("pShaderDESC", paramNameList[0]);
            Assert.Equal("shaderDESCRef", paramNameList[1]);
            Assert.Equal("shaderDescriptionRef", paramNameList[2]);
            Assert.Equal("p0", paramNameList[3]);
            Assert.Equal("p01", paramNameList[4]);
            Assert.Equal("arg0", paramNameList[5]);
            Assert.Equal("@break", paramNameList[6]);
            Assert.Equal("@void", paramNameList[7]);
            Assert.Equal("text", paramNameList[8]);
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
    }
}
