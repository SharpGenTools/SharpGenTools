using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Parsing
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
    }
}
