using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class EnumTests : TestBase
    {
        public EnumTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void BasicCppEnumMapsToCSharpEnum()
        {
            var testDirectory = GenerateTestDirectory();

            var config = new Config.ConfigFile
            {
                Assembly = nameof(BasicCppEnumMapsToCSharpEnum),
                Namespace = nameof(BasicCppEnumMapsToCSharpEnum),
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
                    ")
                }
            };

            (bool success, string output) = RunWithConfig(config);
            AssertRanSuccessfully(success, output);
            var compilation = GetCompilationForGeneratedCode();
            var enumType = compilation.GetTypeByMetadataName($"{nameof(BasicCppEnumMapsToCSharpEnum)}.TestEnum");
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), enumType.EnumUnderlyingType);
            AssertEnumMemberCorrect(enumType, "Element1", 0);
            AssertEnumMemberCorrect(enumType, "Element2", 1);
        }

        [Fact]
        public void CanCreateCSharpEnumFromCppMacros()
        {
            var config = new Config.ConfigFile
            {
                Assembly = nameof(CanCreateCSharpEnumFromCppMacros),
                Namespace = nameof(CanCreateCSharpEnumFromCppMacros),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
#                       define TESTENUM_Element1 0
#                       define TESTENUM_Element2 1
                    "),
                },
                Extension = new List<Config.ConfigBaseRule>
                {
                    new Config.ContextRule("cppEnum"),
                    new Config.ContextRule(nameof(CanCreateCSharpEnumFromCppMacros)),
                    new Config.ContextRule($"{nameof(CanCreateCSharpEnumFromCppMacros)}-ext"),
                    new Config.CreateCppExtensionRule
                    {
                        Enum = "SHARPGEN_TESTENUM",
                        Macro = "TESTENUM_(.*)"
                    },
                    new Config.ClearContextRule(),
                },
                Mappings =
                {
                    new Config.ContextRule("cppEnum"),
                    new Config.ContextRule(nameof(CanCreateCSharpEnumFromCppMacros)),
                    new Config.ContextRule($"{nameof(CanCreateCSharpEnumFromCppMacros)}-ext"),
                    new Config.MappingRule
                    {
                        Enum = "SHARPGEN_TESTENUM",
                        MappingName = "TestEnum",
                        IsFinalMappingName = true,
                        Assembly = nameof(CanCreateCSharpEnumFromCppMacros),
                        Namespace = nameof(CanCreateCSharpEnumFromCppMacros)
                    },
                    new Config.MappingRule
                    {
                        EnumItem = "TESTENUM_(.*)",
                        MappingName = "$1"
                    },
                    new Config.ClearContextRule()
                }
            };

            (bool success, string output) = RunWithConfig(config);
            AssertRanSuccessfully(success, output);
            var compilation = GetCompilationForGeneratedCode();
            var enumType = compilation.GetTypeByMetadataName($"{nameof(CanCreateCSharpEnumFromCppMacros)}.TestEnum");
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), enumType.EnumUnderlyingType);
            AssertEnumMemberCorrect(enumType, "Element1", 0);
            AssertEnumMemberCorrect(enumType, "Element2", 1);
        }

        [Fact]
        public void CppScopedEnumMapsToCSharpEnum()
        {
            var testDirectory = GenerateTestDirectory();

            var config = new Config.ConfigFile
            {
                Assembly = nameof(CppScopedEnumMapsToCSharpEnum),
                Namespace = nameof(CppScopedEnumMapsToCSharpEnum),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum class TestEnum {
                            Element1,
                            Element2
                        };
                    ")
                }
            };

            (bool suiccess, string output) = RunWithConfig(config);
            AssertRanSuccessfully(suiccess, output);
            var compilation = GetCompilationForGeneratedCode();
            var enumType = compilation.GetTypeByMetadataName($"{nameof(CppScopedEnumMapsToCSharpEnum)}.TestEnum");
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), enumType.EnumUnderlyingType);
            AssertEnumMemberCorrect(enumType, "Element1", 0);
            AssertEnumMemberCorrect(enumType, "Element2", 1);
        }

        [Fact]
        public void CppEnumWithExplicitValuesMapsToCSharpEnumWithCorrectValue()
        {
            var testDirectory = GenerateTestDirectory();

            var config = new Config.ConfigFile
            {
                Assembly = nameof(CppEnumWithExplicitValuesMapsToCSharpEnumWithCorrectValue),
                Namespace = nameof(CppEnumWithExplicitValuesMapsToCSharpEnumWithCorrectValue),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum TestEnum {
                            Element1 = 10,
                            Element2 = 15,
                            Element3 = 10
                        };
                    ")
                }
            };

            (var success, string output) = RunWithConfig(config);
            AssertRanSuccessfully(success, output);
            var compilation = GetCompilationForGeneratedCode();
            var enumType = compilation.GetTypeByMetadataName($"{nameof(CppEnumWithExplicitValuesMapsToCSharpEnumWithCorrectValue)}.TestEnum");
            Assert.NotNull(enumType.EnumUnderlyingType);
            AssertEnumMemberCorrect(enumType, "Element1", 10);
            AssertEnumMemberCorrect(enumType, "Element2", 15);
            AssertEnumMemberCorrect(enumType, "Element3", 10);
        }

        [Fact(Skip = "CastXML in GCCXml compat mode does not support C++11 and newer features.")]
        public void CppEnumWithDifferentUnderlyingTypeMapsToCSharpEnum()
        {
            var testDirectory = GenerateTestDirectory();

            var config = new Config.ConfigFile
            {
                Assembly = nameof(CppEnumWithDifferentUnderlyingTypeMapsToCSharpEnum),
                Namespace = nameof(CppEnumWithDifferentUnderlyingTypeMapsToCSharpEnum),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum TestEnum : short {
                            Element
                        };
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("short", "System.Int16"),
                }
            };

            (var success, string output) = RunWithConfig(config);
            AssertRanSuccessfully(success, output);
            var compilation = GetCompilationForGeneratedCode();
            var enumType = compilation.GetTypeByMetadataName($"{nameof(CppEnumWithDifferentUnderlyingTypeMapsToCSharpEnum)}.TestEnum");
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int16), enumType.EnumUnderlyingType);
        }

        private static void AssertEnumMemberCorrect(INamedTypeSymbol enumType, string enumMemberName, int enumMemberValue)
        {
            var enumMemberSymbol = enumType.GetMembers(enumMemberName).Single();
            Assert.True(enumMemberSymbol is IFieldSymbol);
            var element1Field = (IFieldSymbol)enumMemberSymbol;
            Assert.Equal(enumType, element1Field.Type);
            Assert.Equal(enumMemberValue, (int)element1Field.ConstantValue);
        }
    }
}
