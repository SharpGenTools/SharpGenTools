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
    public class Struct : MappingTestBase
    {
        public Struct(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Simple()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Assembly = nameof(Simple),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "simple.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var cppStruct = new CppStruct
            {
                Name = "struct"
            };

            cppStruct.Add(new CppField
            {
                Name = "field",
                TypeName = "int"
            });

            var cppInclude = new CppInclude
            {
                Name = "simple"
            };

            var cppModule = new CppModule();

            cppModule.Add(cppInclude);
            cppInclude.Add(cppStruct);

            var (solution, _) = MapModel(cppModule, config);

            Assert.Single(solution.EnumerateDescendants().OfType<CsStruct>());

            var csStruct = solution.EnumerateDescendants().OfType<CsStruct>().First();

            Assert.Single(csStruct.Fields.Where(fld => fld.Name == "Field"));

            var field = csStruct.Fields.First(fld => fld.Name == "Field");

            Assert.IsType<CsFundamentalType>(field.PublicType);

            var fundamental = (CsFundamentalType)field.PublicType;

            Assert.Equal(typeof(int), fundamental.Type);
        }


        [Fact]
        public void SequentialOffsets()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(SequentialOffsets),
                Namespace = nameof(SequentialOffsets),
                Assembly = nameof(SequentialOffsets),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "simple.h",
                        Attach = true,
                        Namespace = nameof(SequentialOffsets)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var cppStruct = new CppStruct
            {
                Name = "struct"
            };

            cppStruct.Add(new CppField
            {
                Name = "field",
                TypeName = "int"
            });

            cppStruct.Add(new CppField
            {
                Name = "field2",
                TypeName = "int",
                Offset = 1
            });

            var cppInclude = new CppInclude
            {
                Name = "simple"
            };

            var cppModule = new CppModule();

            cppModule.Add(cppInclude);
            cppInclude.Add(cppStruct);

            var (solution, _) = MapModel(cppModule, config);

            var csStruct = solution.EnumerateDescendants().OfType<CsStruct>().First();

            var field = csStruct.Fields.First(fld => fld.Name == "Field");
            var field2 = csStruct.Fields.First(fld => fld.Name == "Field2");

            Assert.Equal(0, field.Offset);

            Assert.Equal(4, field2.Offset);
        }

        [Fact]
        public void InheritingStructs()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(InheritingStructs),
                Namespace = nameof(InheritingStructs),
                Assembly = nameof(InheritingStructs),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "struct.h",
                        Attach = true,
                        Namespace = nameof(SequentialOffsets)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var baseStruct = new CppStruct
            {
                Name = "base"
            };

            baseStruct.Add(new CppField
            {
                Name = "field",
                TypeName = "int"
            });

            var inheritingStruct = new CppStruct
            {
                Name = "inheriting",
                Base = "base"
            };

            inheritingStruct.Add(new CppField
            {
                Name = "field2",
                TypeName = "int",
                Offset = 1
            });

            var cppInclude = new CppInclude
            {
                Name = "struct"
            };

            var cppModule = new CppModule();

            cppModule.Add(cppInclude);
            cppInclude.Add(baseStruct);
            cppInclude.Add(inheritingStruct);

            var (solution, _) = MapModel(cppModule, config);

            var csStruct = solution.EnumerateDescendants().OfType<CsStruct>().First(@struct => @struct.Name == "Inheriting");

            var field = csStruct.Fields.First(fld => fld.Name == "Field");
            var field2 = csStruct.Fields.First(fld => fld.Name == "Field2");

            Assert.Equal(0, field.Offset);

            Assert.Equal(4, field2.Offset);
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
