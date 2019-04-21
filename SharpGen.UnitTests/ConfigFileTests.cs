using SharpGen.Config;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class ConfigFileTests : TestBase
    {
        public ConfigFileTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void VariableSubstitutionSubstitutesVariableValues()
        {
            var config = new ConfigFile
            {
                IncludeDirs =
                {
                    new IncludeDirRule("$(TEST_VARIABLE)")
                }
            };

            config.Variables.Add(new KeyValue("TEST_VARIABLE", "Hello World!"));

            config.ExpandVariables(false, Logger);

            Assert.Equal("Hello World!", config.IncludeDirs[0].Path);
        }

        [Fact]
        public void DynamicVariablesNotSubstitutedWhenExpandIsFalse()
        {
            var config = new ConfigFile
            {
                IncludeDirs =
                {
                    new IncludeDirRule("#(TEST_VARIABLE)")
                }
            };

            config.DynamicVariables.Add("TEST_VARIABLE", "Hello World!");

            config.ExpandVariables(false, Logger);

            Assert.Equal("#(TEST_VARIABLE)", config.IncludeDirs[0].Path);
        }

        [Fact]
        public void DynamicVariablesSubstitutedWhenExpandIsTrue()
        {
            var config = new ConfigFile
            {
                IncludeDirs =
                {
                    new IncludeDirRule("#(TEST_VARIABLE)")
                }
            };

            config.DynamicVariables.Add("TEST_VARIABLE", "Hello World!");

            config.ExpandVariables(true, Logger);

            Assert.Equal("Hello World!", config.IncludeDirs[0].Path);
        }
    }
}