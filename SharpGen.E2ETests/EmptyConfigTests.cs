using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class EmptyConfigTests : TestBase
    {
        public EmptyConfigTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void EmptyConfigSucceeds()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile { };
            Assert.True(RunWithConfig(config, failTestOnError: false).success);
        }
    }
}
