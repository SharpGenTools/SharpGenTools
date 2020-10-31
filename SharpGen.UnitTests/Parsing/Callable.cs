using SharpGen.Config;
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
            var config = new ConfigFile
            {
                Id = nameof(CppParserCorrectlyParsesStdCall),
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

        [Fact]
        public void CppParserCorrectlyInfersCdeclForDefaultCallingConvention()
        {
            var config = new ConfigFile
            {
                Id = nameof(CppParserCorrectlyInfersCdeclForDefaultCallingConvention),
                Namespace = nameof(CppParserCorrectlyInfersCdeclForDefaultCallingConvention),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("function", @"
                        void func();
                    "),
                }
            };

            var model = ParseCpp(config);

            var function = model.FindFirst<CppFunction>("func");

            Assert.Equal(CppCallingConvention.CDecl, function.CallingConvention);
        }

        [Fact]
        public void UnnamedParameterAssignedDefaultName()
        {
            var config = new ConfigFile
            {
                Id = nameof(CppParserCorrectlyParsesStdCall),
                Namespace = nameof(CppParserCorrectlyParsesStdCall),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("function", @"
                        void func(int);
                    "),
                }
            };

            var model = ParseCpp(config);

            Assert.NotNull(model.FindFirst<CppParameter>("func::arg0"));
        }

        [Theory]
        [InlineData("*")]
        [InlineData("&")]
        public void PointerAndReferenceParametersAreCorrectlyParsed(string pointerSymbol)
        {
            var config = new ConfigFile
            {
                Id = nameof(PointerAndReferenceParametersAreCorrectlyParsed),
                Namespace = nameof(PointerAndReferenceParametersAreCorrectlyParsed),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("function", $@"
                        void func(int{pointerSymbol} a);
                    "),
                }
            };

            var model = ParseCpp(config);
            Assert.Equal(pointerSymbol, model.FindFirst<CppParameter>("func::a").Pointer);
        }
    }
}
