using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class EmptyConfigTests : E2ETestBase
    {
        public EmptyConfigTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void EmptyConfigSucceeds()
        {
            var config = new Config.ConfigFile();
            var (success, output) = RunWithConfig(config);
            AssertRanSuccessfully(success, output);
        }
    }
}
