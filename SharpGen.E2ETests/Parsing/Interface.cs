using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Parsing
{
    public class Interface : ParsingTestBase
    {
        public Interface(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Inheriting()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(Inheriting),
                Assembly = nameof(Inheriting),
                Namespace = nameof(Inheriting),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("interfaces", @"
                        struct Base
                        {
                            virtual int BaseMethod() = 0;
                        };
                        struct Inheriting : public Base
                        {
                            virtual int Method() = 0;
                        };
                    "),
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var model = ParseCpp(config);

            var method = model.FindFirst<CppMethod>("Inheriting::Method");

            Assert.NotNull(method);
            Assert.Equal(1, method.Offset);
        }
    }
}
