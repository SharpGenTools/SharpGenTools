using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class Function : MappingTestBase
    {
        public Function(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Basic()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(Basic),
                Namespace = nameof(Basic),
                Assembly = nameof(Basic),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        Attach = true,
                        File = "func.h",
                        Namespace = nameof(Basic)
                    }
                },
                Extension =
                {
                    new Config.CreateExtensionRule
                    {
                        NewClass = $"{nameof(Basic)}.Functions",
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Function = "Test",
                        FunctionDllName = "\"Test.dll\"",
                        Group = $"{nameof(Basic)}.Functions"
                    }
                }
            };

            var function = new CppFunction
            {
                Name = "Test",
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int",
                }
            };

            var include = new CppInclude
            {
                Name = "func"
            };

            var module = new CppModule();

            include.Add(function);
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsGroup>());
            
            var group = solution.EnumerateDescendants().OfType<CsGroup>().First();
            Assert.Equal("Functions", group.Name);

            Assert.Single(group.Functions);

            var csFunc = group.Functions.First();
            Assert.Equal(typeof(int), ((CsFundamentalType)csFunc.ReturnValue.PublicType).Type);
            Assert.Empty(csFunc.Parameters);
            Assert.Equal(Visibility.Static, csFunc.Visibility & Visibility.Static);
        }
    }
}
