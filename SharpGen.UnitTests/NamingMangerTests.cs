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
            Assert.Equal("ShaderDescription", manager.Rename(new CppElement
            {
                Name = "SHADER_DESC"
            }));
        }

        [Fact]
        public void LongerRuleTakesPrecedent()
        {
            var manager = new NamingRulesManager();

            manager.AddShortNameRule("INFO", "Information");
            manager.AddShortNameRule("INFORMATION", "Information");

            Assert.Equal("RawDeviceInformation", manager.Rename(new CppElement
            {
                Name = "RAW_DEVICE_INFO"
            }));

            Assert.Equal("RawDeviceInformation", manager.Rename(new CppElement
            {
                Name = "RAW_DEVICE_INFORMATION"
            }));

        }
    }
}
