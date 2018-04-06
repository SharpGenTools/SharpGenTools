using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public class Struct : ParsingTestBase
    {
        public Struct(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void SequentialFieldsOffsets()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(SequentialFieldsOffsets),
                Namespace = nameof(SequentialFieldsOffsets),
                Assembly = nameof(SequentialFieldsOffsets),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("sequentialFields", @"
                        struct Test {
                            int field1;
                            int field2;
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            var generatedStruct = model.FindFirst<CppStruct>("Test");

            var field = generatedStruct.FindFirst<CppField>("Test::field1");

            Assert.NotNull(field);
            Assert.Equal(0, field.Offset);

            var field2 = generatedStruct.FindFirst<CppField>("Test::field2");

            Assert.NotNull(field2);
            Assert.Equal(1, field2.Offset);
        }

        [Fact]
        public void UnionFieldOffsets0()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(UnionFieldOffsets0),
                Namespace = nameof(UnionFieldOffsets0),
                Assembly = nameof(UnionFieldOffsets0),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("union", @"
                        union Test {
                            int field1;
                            int field2;
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            var generatedStruct = model.FindFirst<CppStruct>("Test");

            var field = generatedStruct.FindFirst<CppField>("Test::field1");

            Assert.NotNull(field);
            Assert.Equal(0, field.Offset);

            var field2 = generatedStruct.FindFirst<CppField>("Test::field2");

            Assert.NotNull(field2);
            Assert.Equal(0, field2.Offset);
        }

        [Fact]
        public void BitfieldStructHasCorrectBitOffsets()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(BitfieldStructHasCorrectBitOffsets),
                Namespace = nameof(BitfieldStructHasCorrectBitOffsets),
                Assembly = nameof(BitfieldStructHasCorrectBitOffsets),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("bitfield", @"
                        struct Test
                        {
                            int bitfield1 : 8;
                            int bitfield2 : 6;
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            var generatedStruct = model.FindFirst<CppStruct>("Test");

            var firstBitField = generatedStruct.FindFirst<CppField>("Test::bitfield1");

            Assert.Equal(8, firstBitField.BitOffset);
            Assert.True(firstBitField.IsBitField);
            Assert.Equal(0, firstBitField.Offset);

            var secondBitField = generatedStruct.FindFirst<CppField>("Test::bitfield2");

            Assert.Equal(6, secondBitField.BitOffset);
            Assert.True(secondBitField.IsBitField);
            Assert.Equal(0, secondBitField.Offset);
        }

        [Fact(Skip = "Issue #48")]
        public void MultipleBitfieldsHaveCorrectBitOffsets()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(MultipleBitfieldsHaveCorrectBitOffsets),
                Namespace = nameof(MultipleBitfieldsHaveCorrectBitOffsets),
                Assembly = nameof(MultipleBitfieldsHaveCorrectBitOffsets),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("multibitfield", @"
                        struct Test
                        {
                            int bitfield1 : 16;
                            int field;
                            int bitfield2 : 16;
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            var generatedStruct = model.FindFirst<CppStruct>("Test");

            var firstBitField = generatedStruct.FindFirst<CppField>("Test::bitfield1");

            Assert.Equal(16, firstBitField.BitOffset);
            Assert.True(firstBitField.IsBitField);
            Assert.Equal(0, firstBitField.Offset);

            var field = generatedStruct.FindFirst<CppField>("Test::field");

            Assert.Equal(1, field.Offset);

            var secondBitField = generatedStruct.FindFirst<CppField>("Test::bitfield2");

            Assert.Equal(16, secondBitField.BitOffset);
            Assert.True(secondBitField.IsBitField);
            Assert.Equal(2, secondBitField.Offset);
        }

        [Fact]
        public void InnerStructGivenExpectedName()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(InnerStructGivenExpectedName),
                Namespace = nameof(InnerStructGivenExpectedName),
                Assembly = nameof(InnerStructGivenExpectedName),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("anonStruct", @"
                        struct Test {
                            struct { int i; } field1;
                        };
                    ")
                }
            };

            var model = ParseCpp(config);

            var generatedStruct = model.FindFirst<CppStruct>("Test");

            var field = generatedStruct.FindFirst<CppField>("Test::field1");

            Assert.NotNull(field);
            Assert.Equal("Test_INNER_0", field.TypeName);
        }

        [Fact]
        public void TypedefedStructAdjustsNameToTypedef()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(TypedefedStructAdjustsNameToTypedef),
                Namespace = nameof(TypedefedStructAdjustsNameToTypedef),
                Assembly = nameof(TypedefedStructAdjustsNameToTypedef),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("typedef", @"
                        typedef struct _Test {
                            int field1;
                            int field2;
                        } Test;

                        typedef struct tagTest2 {
                            int field1;
                            int field2;
                        } Test2;

                        typedef struct {
                            int field1;
                            int field2;
                        } Test3;
                    ")
                }
            };

            var model = ParseCpp(config);

            Assert.NotNull(model.FindFirst<CppStruct>("Test"));
            Assert.NotNull(model.FindFirst<CppStruct>("Test2"));
            Assert.NotNull(model.FindFirst<CppStruct>("Test3"));
        }
    }
}
