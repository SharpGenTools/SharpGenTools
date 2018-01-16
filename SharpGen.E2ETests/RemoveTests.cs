using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class RemoveTests : E2ETestBase
    {
        public RemoveTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void RemoveWithRegexRemovesMatchingElements()
        {
            var config = new Config.ConfigFile
            {
                Assembly = nameof(RemoveWithRegexRemovesMatchingElements),
                Namespace = nameof(RemoveWithRegexRemovesMatchingElements),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum TestEnum {
                            Element1,
                            Element2
                        };
                        enum TEST_ENUM1_REMOVE {
                            Element
                        };

                        enum TEST_ENUM2_REMOVE {
                            Element3
                        };
                    ")
                },
                Mappings =
                {
                    new Config.RemoveRule
                    {
                        Enum=@"TEST_ENUM(\d+)_.*"
                    }
                }
            };


            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();

            Assert.Null(compilation.GetTypeByMetadataName($"{nameof(RemoveWithRegexRemovesMatchingElements)}.TestEnum1Remove"));
        }
    }
}
