using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
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
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "simple.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
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
            var config = new ConfigFile
            {
                Id = nameof(SequentialOffsets),
                Namespace = nameof(SequentialOffsets),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "simple.h",
                        Attach = true,
                        Namespace = nameof(SequentialOffsets)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
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
            var config = new ConfigFile
            {
                Id = nameof(InheritingStructs),
                Namespace = nameof(InheritingStructs),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "struct.h",
                        Attach = true,
                        Namespace = nameof(SequentialOffsets)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
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
        public void IntFieldMappedToBoolIsMarkedAsBoolToInt()
        {
            var structName = "BoolToInt";
            var config = new ConfigFile
            {
                Id = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt),
                Namespace = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(IntFieldMappedToBoolIsMarkedAsBoolToInt)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32"),
                    new BindRule("bool", "System.Boolean")
                },
                Mappings =
                {
                    new MappingRule
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

        [Fact]
        public void MultipleBitfieldOffsetsGeneratedCorrectly()
        {
            var config = new ConfigFile
            {
                Id = nameof(MultipleBitfieldOffsetsGeneratedCorrectly),
                Namespace = nameof(MultipleBitfieldOffsetsGeneratedCorrectly),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(MultipleBitfieldOffsetsGeneratedCorrectly)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                }
            };


            var structure = new CppStruct
            {
                Name = "Test"
            };

            structure.Add(new CppField
            {
                Name = "bitfield1",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 0,
            });
            structure.Add(new CppField
            {
                Name = "field",
                TypeName = "int",
                Offset = 1,
            });
            structure.Add(new CppField
            {
                Name = "bitfield2",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 2
            });

            var include = new CppInclude
            {
                Name = "test"
            };

            include.Add(structure);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            var csStruct = solution.EnumerateDescendants().OfType<CsStruct>().First();

            var bitField1 = csStruct.Fields.First(field => field.Name == "Bitfield1");

            Assert.Equal(0, bitField1.BitOffset);
            Assert.Equal(0, bitField1.Offset);
            Assert.Equal((1 << 16) - 1, bitField1.BitMask);
            Assert.True(bitField1.IsBitField);

            var csField = csStruct.Fields.First(field => field.Name == "Field");

            Assert.Equal(4, csField.Offset);
            Assert.False(csField.IsBitField);

            var bitField2 = csStruct.Fields.First(field => field.Name == "Bitfield2");

            Assert.Equal(0, bitField2.BitOffset);
            Assert.Equal(8, bitField2.Offset);
            Assert.Equal((1 << 16) - 1, bitField2.BitMask);
            Assert.True(bitField2.IsBitField);
        }

        [Fact]
        public void UnionsWithPointersGeneratesStructure()
        {
            var config = new ConfigFile
            {
                Id = nameof(UnionsWithPointersGeneratesStructure),
                Namespace = nameof(UnionsWithPointersGeneratesStructure),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(UnionsWithPointersGeneratesStructure)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                }
            };


            var structure = new CppStruct
            {
                Name = "Test",
                IsUnion = true
            };

            structure.Add(new CppField
            {
                Name = "pointer",
                TypeName = "int",
                Pointer = "*"
            });

            structure.Add(new CppField
            {
                Name = "scalar",
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

            var csStruct = solution.EnumerateDescendants().OfType<CsStruct>().First();

            foreach (var field in csStruct.Fields)
            {
                Assert.Equal(0, field.Offset);
            }

            Assert.False(Logger.HasErrors);
        }

        [Fact]
        public void NonPortableStructAlignmentRaisesError()
        {
            var config = new ConfigFile
            {
                Id = nameof(NonPortableStructAlignmentRaisesError),
                Namespace = nameof(NonPortableStructAlignmentRaisesError),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(NonPortableStructAlignmentRaisesError)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                }
            };


            var structure = new CppStruct
            {
                Name = "Test"
            };

            structure.Add(new CppField
            {
                Name = "bitfield1",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 0,
            });
            structure.Add(new CppField
            {
                Name = "bitfield2",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 0
            });

            structure.Add(new CppField
            {
                Name = "pointer",
                TypeName = "int",
                Pointer = "*",
                Offset = 1
            });

            structure.Add(new CppField
            {
                Name = "field",
                TypeName = "int",
                Offset = 2,
            });

            var include = new CppInclude
            {
                Name = "test"
            };

            include.Add(structure);

            var module = new CppModule();
            module.Add(include);

            using (LoggerMessageCountEnvironment(1, LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.NonPortableAlignment))
            {
                MapModel(module, config);
            }
        }

        [Fact]
        public void NonPortableLayoutDoesNotErrorWhenMarkedForCustomMarshalling()
        {
            var config = new ConfigFile
            {
                Id = nameof(NonPortableLayoutDoesNotErrorWhenMarkedForCustomMarshalling),
                Namespace = nameof(NonPortableLayoutDoesNotErrorWhenMarkedForCustomMarshalling),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "test.h",
                        Attach = true,
                        Namespace = nameof(NonPortableLayoutDoesNotErrorWhenMarkedForCustomMarshalling)
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
                        Struct = "Test",
                        StructCustomMarshal = true
                    }
                }
            };


            var structure = new CppStruct
            {
                Name = "Test"
            };

            structure.Add(new CppField
            {
                Name = "bitfield1",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 0,
            });
            structure.Add(new CppField
            {
                Name = "bitfield2",
                TypeName = "int",
                IsBitField = true,
                BitOffset = 16,
                Offset = 0
            });

            structure.Add(new CppField
            {
                Name = "pointer",
                TypeName = "int",
                Pointer = "*",
                Offset = 1
            });

            structure.Add(new CppField
            {
                Name = "field",
                TypeName = "int",
                Offset = 2,
            });

            var include = new CppInclude
            {
                Name = "test"
            };

            include.Add(structure);

            var module = new CppModule();
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.False(Logger.HasErrors);
        }
    }
}
