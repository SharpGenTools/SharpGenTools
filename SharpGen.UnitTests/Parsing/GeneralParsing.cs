using System.Collections.Generic;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
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
            var config = new ConfigFile
            {
                Id = nameof(InvalidCppErrorsLogger),
                Namespace = nameof(InvalidCppErrorsLogger),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("invalid", "struct Test { InvalidType test; };")
                }
            };

            using (LoggerMessageCountEnvironment(3, LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.CastXmlError))
            {
                ParseCpp(config);
            }
        }

        [Fact]
        public void PartialAttachOnlyAddsAttachedTypesToModel()
        {

            var config = new ConfigFile
            {
                Id = nameof(InvalidCppErrorsLogger),
                Namespace = nameof(InvalidCppErrorsLogger),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("invalid", @"
                        struct Test {};
                        struct UnAttached {};
                        enum UnAttached2 { Element1 };
                    ", new List<string>{ "Test" })
                }
            };

            var model = ParseCpp(config);

            Assert.NotNull(model.FindFirst<CppStruct>("Test"));
            Assert.Null(model.FindFirst<CppStruct>("UnAttached"));
            Assert.Null(model.FindFirst<CppEnum>("UnAttached2"));
        }
    }
}
