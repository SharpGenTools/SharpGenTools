using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public class Enum : ParsingTestBase
    {
        public Enum(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void CreatedFromMacros()
        {
            var config = new ConfigFile
            {
                Id = nameof(CreatedFromMacros),
                Namespace = nameof(CreatedFromMacros),
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
                Extension =
                {
                    new ContextRule("cppEnum"),
                    new ContextRule(nameof(CreatedFromMacros)),
                    new ContextRule($"{nameof(CreatedFromMacros)}-ext"),
                    new CreateCppExtensionRule
                    {
                        Enum = "SHARPGEN_TESTENUM",
                        Macro = "TESTENUM_(.*)"
                    },
                    new ClearContextRule(),
                }
            };

            var model = ParseCpp(config);

            var generatedEnum = model.FindFirst<CppEnum>("SHARPGEN_TESTENUM");

            Assert.NotNull(generatedEnum);

            Assert.Single(generatedEnum.EnumItems.Where(item => item.Name == "TESTENUM_Element1" && item.Value == "0"));
            Assert.Single(generatedEnum.EnumItems.Where(item => item.Name == "TESTENUM_Element2" && item.Value == "1"));
        }

        [Fact]
        public void ScopedEnum()
        {
            var config = new ConfigFile
            {
                Id = nameof(ScopedEnum),
                Namespace = nameof(ScopedEnum),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum class TestEnum {
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            Assert.Single(model.Find<CppEnum>("TestEnum"));
        }

        [Fact(Skip = "CastXML in GCCXml compat mode does not support C++11 and newer features.")]
        public void SpecifiedUnderlyingType()
        {
            var config = new ConfigFile
            {
                Id = nameof(SpecifiedUnderlyingType),
                Namespace = nameof(SpecifiedUnderlyingType),
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
                    new BindRule("short", "System.Int16"),
                }
            };

            var model = ParseCpp(config);

            var cppEnum = model.FindFirst<CppEnum>("TestEnum");

            Assert.Equal("short", cppEnum.UnderlyingType);
        }


        [Fact]
        public void AnonymousEnumAssignedExpectedName()
        {
            var config = new ConfigFile
            {
                Id = nameof(AnonymousEnumAssignedExpectedName),
                Namespace = nameof(AnonymousEnumAssignedExpectedName),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("cppEnum", @"
                        enum {
                            Element1
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            Assert.Single(model.Find<CppEnum>("CPPENUM_ENUM_0"));
        }
    }
}
