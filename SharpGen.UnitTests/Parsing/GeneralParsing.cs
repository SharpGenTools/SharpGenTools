using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public class GeneralParsing : ParsingTestBase
    {
        public GeneralParsing(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void InvalidCppErrorsLogger()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(InvalidCppErrorsLogger),
                Assembly = nameof(InvalidCppErrorsLogger),
                Namespace = nameof(InvalidCppErrorsLogger),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("invalid", "struct Test { InvalidType test; };")
                }
            };

            var model = ParseCpp(config);

            Assert.True(Logger.HasErrors);
        }
    }
}
