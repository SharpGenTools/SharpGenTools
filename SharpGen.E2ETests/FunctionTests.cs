using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class FunctionTests : TestBase
    {
        public FunctionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void SimpleFunctionMapsCorrectly()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(SimpleFunctionMapsCorrectly),
                Assembly = nameof(SimpleFunctionMapsCorrectly),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("simpleFunction", @"
                        int Test();
                    ")
                },
                Extension =
                {
                    new Config.CreateExtensionRule
                    {
                        NewClass = $"{nameof(SimpleFunctionMapsCorrectly)}.Functions",
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Function = "Test",
                        FunctionDllName = "\"Test.dll\"",
                        CsClass = $"{nameof(SimpleFunctionMapsCorrectly)}.Functions"
                    }
                }
            };
            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var group = compilation.GetTypeByMetadataName($"{nameof(SimpleFunctionMapsCorrectly)}.Functions");
            var method = (IMethodSymbol)group.GetMembers("Test").Single();
            Assert.Equal(compilation.GetTypeByMetadataName("System.Int32"), method.ReturnType);
            Assert.True(method.IsStatic);
        }
    }
}
