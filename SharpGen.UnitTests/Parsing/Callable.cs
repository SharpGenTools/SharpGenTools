using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.CppModel;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public class Callable : ParsingTestBase
    {
        public Callable(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void CppParserCorrectlyParsesStdCall()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(CppParserCorrectlyParsesStdCall),
                Assembly = nameof(CppParserCorrectlyParsesStdCall),
                Namespace = nameof(CppParserCorrectlyParsesStdCall),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("function", @"
                        void __stdcall func();
                    "),
                }
            };

            var model = ParseCpp(config);

            var function = model.FindFirst<CppFunction>("func");

            Assert.Equal(CppCallingConvention.StdCall, function.CallingConvention);
        }
    }
}
