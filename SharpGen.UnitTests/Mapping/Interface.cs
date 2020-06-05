using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class Interface : MappingTestBase
    {
        public Interface(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Simple()
        {
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                }
            };

            var iface = new CppInterface
            {
                Name = "Interface",
                TotalMethodCount = 1
            };

            iface.Add(new CppMethod
            {
                Name = "method",
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            });

            var include = new CppInclude
            {
                Name = "interface"
            };

            include.Add(iface);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsInterface>());

            var csIface = solution.EnumerateDescendants().OfType<CsInterface>().First();

            Assert.Single(csIface.Methods);

            var method = csIface.Methods.First();

            Assert.Equal(0, method.Offset);

            Assert.IsType<CsFundamentalType>(method.ReturnValue.PublicType);

            Assert.Equal(typeof(int), ((CsFundamentalType)method.ReturnValue.PublicType).Type);
        }

        [Fact]
        public void DualCallbackFlowsNativeImplementation()
        {
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                },
                Mappings =
                {
                    new MappingRule
                    {
                        Interface = "Interface",
                        IsCallbackInterface = true,
                        IsDualCallbackInterface = true
                    }
                }
            };

            var iface = new CppInterface
            {
                Name = "Interface",
                TotalMethodCount = 1
            };

            iface.Add(new CppMethod
            {
                Name = "method",
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            });

            var include = new CppInclude
            {
                Name = "interface"
            };

            include.Add(iface);

            var module = new CppModule();
            module.Add(include);

            var (_, defines) = GetConsumerBindings(module, config);

            var interfaceDefine = defines.First(define => define.Interface == "Simple.Interface");

            Assert.Equal("Simple.InterfaceNative", interfaceDefine.NativeImplementation);
        }

        [Fact]
        public void DefineWithNativeImplementationDefinesNativeImplementationType()
        {
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Extension =
                {
                    new DefineExtensionRule
                    {
                        Interface = "Imported.Param",
                        NativeImplementation = "Imported.ParamNative"
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32"),
                    new BindRule("Param", "Imported.Param")
                }
            };

            var iface = new CppInterface
            {
                Name = "Interface",
                TotalMethodCount = 1
            };

            var method = new CppMethod
            {
                Name = "method",
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            };

            method.Add(new CppParameter
            {
                Name = "param",
                TypeName = "Param",
                Pointer = "*"
            });

            iface.Add(method);

            var include = new CppInclude
            {
                Name = "interface"
            };

            include.Add(iface);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsParameter>());

            var param = solution.EnumerateDescendants().OfType<CsParameter>().First();

            Assert.IsType<CsInterface>(param.PublicType);

            Assert.NotNull(((CsInterface)param.PublicType).NativeImplementation);

            Assert.False(Logger.HasErrors);
        }
    }
}
