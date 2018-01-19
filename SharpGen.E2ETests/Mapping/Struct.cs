using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Mapping
{
    public class Struct : MappingTestBase
    {
        public Struct(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void AsParameterByValueMarkedAsMarshalToNative()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(AsParameterByValueMarkedAsMarshalToNative),
                Namespace = nameof(AsParameterByValueMarkedAsMarshalToNative),
                Assembly = nameof(AsParameterByValueMarkedAsMarshalToNative),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(AsParameterByValueMarkedAsMarshalToNative)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                },
                Extension =
                {
                    new Config.CreateExtensionRule
                    {
                        NewClass = $"{nameof(AsParameterByValueMarkedAsMarshalToNative)}.Functions",
                        Visibility = Config.Visibility.Public
                    }
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Function = "TestFunction",
                        Group = $"{nameof(AsParameterByValueMarkedAsMarshalToNative)}.Functions",
                        FunctionDllName="Dll"
                    }
                }
            };

            var structure = new CppStruct
            {
                Name = "Struct"
            };

            structure.Add(new CppField
            {
                Name = "field",
                TypeName = "int",
                IsArray = true,
                ArrayDimension = "3"
            });

            var function = new CppFunction
            {
                Name = "TestFunction",
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            };

            function.Add(new CppParameter
            {
                Name = "param",
                TypeName = "Struct"
            });

            var include = new CppInclude
            {
                Name = "test"
            };

            include.Add(structure);
            include.Add(function);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsStruct>().Where(csStruct => csStruct.Name == "Struct"));

            var generatedStruct = solution.EnumerateDescendants().OfType<CsStruct>().First(csStruct => csStruct.Name == "Struct");

            Assert.True(generatedStruct.MarshalledToNative);
        }


        [Fact]
        public void IntFieldMappedToBoolIsMarkedAsBoolToInt()
        {
            var structName = "BoolToInt";
            var config = new Config.ConfigFile
            {
                Id = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt),
                Namespace = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt),
                Assembly = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32"),
                    new Config.BindRule("bool", "System.Boolean")
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Field = $"{structName}::field",
                        MappingType = "bool",
                    },
                }
            };

            var structure = new CppStruct
            {
                Name = structName
            };

            structure.Add(new CppField
            {
                Name = "field",
                TypeName = "int"
            });

            var include = new CppInclude
            {
                Name = "test"
            };

            include.Add(structure);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsStruct>().Where(csStruct => csStruct.Name == structName));

            var generatedStruct = solution.EnumerateDescendants().OfType<CsStruct>().First(csStruct => csStruct.Name == structName);

            Assert.Single(generatedStruct.Fields);

            var field = generatedStruct.Fields.First();

            Assert.True(field.IsBoolToInt);
        }
    }
}
