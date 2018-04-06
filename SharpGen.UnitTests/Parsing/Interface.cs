using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void InheritingFromInterfaceDefinedInUnprocessedIncludeDoesntSetBase()
        {
            var externalCppFile = CreateCppFile("test", @"
                struct Test
                {
                    virtual void Method() = 0;
                };
            ");

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
                    CreateCppFile("attached", @"
                        #include ""test.h""

                        struct Attached : public Test
                        {
                           virtual void Method2() = 0;
                        };
                    "),
                }
            };

            var model = ParseCpp(config);

            var attached = model.FindFirst<CppInterface>("Attached");

            Assert.Null(attached.Base);
        }

        [Fact]
        public void OverloadedMethodsCorrectlyOrderedInVtable()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(OverloadedMethodsCorrectlyOrderedInVtable),
                Assembly = nameof(OverloadedMethodsCorrectlyOrderedInVtable),
                Namespace = nameof(OverloadedMethodsCorrectlyOrderedInVtable),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("overloads", @"
                        struct Test
                        {
                            virtual int Method() = 0;
                            virtual int NotOverloaded() = 0;
                            virtual int Method(int i) = 0;
                        };
                    "),
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var model = ParseCpp(config);
            var methods = model.Find<CppMethod>("Test::Method");

            var parameterized = methods.First(method => method.Parameters.Any());

            Assert.Equal(0, parameterized.Offset);

            var parameterless = methods.First(method => !method.Parameters.Any());

            Assert.Equal(1, parameterless.Offset);

            Assert.Equal(2, model.FindFirst<CppMethod>("Test::NotOverloaded").Offset);
        }


        [Fact]
        public void DefaultMethodCallingConventionIsThisCall()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(DefaultMethodCallingConventionIsThisCall),
                Assembly = nameof(DefaultMethodCallingConventionIsThisCall),
                Namespace = nameof(DefaultMethodCallingConventionIsThisCall),
                IncludeDirs =
                {
                    GetTestFileIncludeRule()
                },
                Includes =
                {
                    CreateCppFile("defaultcc", @"
                        struct Test
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

            Assert.Equal(CppCallingConvention.ThisCall, model.FindFirst<CppMethod>("Test::Method").CallingConvention);
        }
    }
}
