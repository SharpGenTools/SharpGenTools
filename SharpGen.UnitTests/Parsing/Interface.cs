using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
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

        [Fact]
        public void GuidAttribute()
        {
            var guid = Guid.Parse("B31C25F0-44CA-414A-A067-304E3A077184");

            var config = new Config.ConfigFile
            {
                Id = nameof(GuidAttribute),
                Assembly = nameof(GuidAttribute),
                Namespace = nameof(GuidAttribute),
                IncludeProlog =
                {
                    "#define __declspec(x) __attribute__((annotate(#x)))\n"
                },
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("guidTest", $@"
                        struct __declspec(uuid(""{guid.ToString()}""))
                        Test
                        {{
                            virtual void Method() = 0;
                        }};
                    "),
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var model = ParseCpp(config);

            var iface = model.FindFirst<CppInterface>("Test");

            Assert.Equal(guid.ToString(), iface.Guid);
        }
    }
}
