using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SharpGen.E2ETests
{
    public class EmptyConfigTests : TestBase
    {
        [Fact]
        public void EmptyConfigFails()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile { };
            Assert.Equal(1, RunWithConfig(testDirectory, config).exitCode);
        }
    }
}
