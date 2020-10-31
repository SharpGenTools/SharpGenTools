using System;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
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
            var config = new ConfigFile
            {
                Id = nameof(Inheriting),
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
                    new BindRule("int", "System.Int32")
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

            var config = new ConfigFile
            {
                Id = nameof(GuidAttribute),
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
                    new BindRule("int", "System.Int32")
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

            var config = new ConfigFile
            {
                Id = nameof(GuidAttribute),
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
            var config = new ConfigFile
            {
                Id = nameof(OverloadedMethodsCorrectlyOrderedInVtable),
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
                    new BindRule("int", "System.Int32")
                }
            };

            var model = ParseCpp(config);
            var methods = model.Find<CppMethod>("Test::Method");

            var parameterless = methods.First(method => !method.Parameters.Any());

            Assert.Equal(1, parameterless.WindowsOffset);
            Assert.Equal(0, parameterless.Offset);

            var notOverloaded = model.FindFirst<CppMethod>("Test::NotOverloaded");

            Assert.Equal(2, notOverloaded.WindowsOffset);
            Assert.Equal(1, notOverloaded.Offset);

            var parameterized = methods.First(method => method.Parameters.Any());

            Assert.Equal(0, parameterized.WindowsOffset);
            Assert.Equal(2, parameterized.Offset);

        }


        [Fact]
        public void DefaultMethodCallingConventionIsThisCall()
        {
            var config = new ConfigFile
            {
                Id = nameof(DefaultMethodCallingConventionIsThisCall),
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
                    new BindRule("int", "System.Int32")
                }
            };

            var model = ParseCpp(config);

            Assert.Equal(CppCallingConvention.ThisCall, model.FindFirst<CppMethod>("Test::Method").CallingConvention);
        }
    }
}
