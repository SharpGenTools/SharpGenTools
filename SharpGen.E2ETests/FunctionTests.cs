using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class FunctionTests : E2ETestBase
    {
        public FunctionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void SimpleFunctionMapsCorrectly()
        {
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


        [Fact]
        public void FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo()
        {
            var config = new Config.ConfigFile
            {
                Namespace = nameof(FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo),
                Assembly = nameof(FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("boolToInt", @"
                        struct BoolToInt {
                            int test[3];
                        };

                        extern ""C"" bool TestFunction(BoolToInt t);
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32"),
                    new Config.BindRule("bool", "System.Boolean")
                },
                Extension =
                {
                    new Config.CreateExtensionRule
                    {
                        NewClass = $"{nameof(FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo)}.Functions",
                        Visibility = Config.Visibility.Public
                    }
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Field = "BoolToInt::test",
                        MappingType = "bool",
                    },
                    new Config.MappingRule
                    {
                        Function = "TestFunction",
                        CsClass = $"{nameof(FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo)}.Functions",
                        FunctionDllName="Dll"
                    }
                }
            };

            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(FunctionTakingStructParameterThatHasNativeTypeGeneratesMarshalTo)}.BoolToInt");
            var member = structType.GetMembers("Test")[0] as IPropertySymbol;
            Assert.NotNull(member);
            Assert.Equal(TypeKind.Array, member.Type.TypeKind);
            var memberType = (IArrayTypeSymbol)member.Type;
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Boolean), memberType.ElementType);
            var marshalType = structType.GetTypeMembers("__Native")[0];
            Assert.NotNull(marshalType);
            Assert.Single(structType.GetMembers("__MarshalTo"));
        }
    }
}
