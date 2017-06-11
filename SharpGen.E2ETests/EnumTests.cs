using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SharpGen.E2ETests
{
    public class EnumTests : TestBase
    {
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
                    CreateCppFile(testDirectory, "cppEnum", @"
                        enum TestEnum {
                            Element1,
                            Element2
                        };
                    ")
                }
            };

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
            var enumType = compilation.GetTypeByMetadataName($"{nameof(BasicCppEnumMapsToCSharpEnum)}.TestEnum");
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
                    CreateCppFile(testDirectory, "cppEnum", @"
                        enum class TestEnum {
                            Element1,
                            Element2
                        };
                    ")
                }
            };

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
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
                    CreateCppFile(testDirectory, "cppEnum", @"
                        enum TestEnum {
                            Element1 = 10,
                            Element2 = 15,
                            Element3 = 10
                        };
                    ")
                }
            };

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
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
                    CreateCppFile(testDirectory, "cppEnum", @"
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

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
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
