using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SharpGen.E2ETests
{
    public class StructTests : TestBase
    {
        [Fact]
        public void SimpleCppStructGeneratesCorrectCSharpStruct()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(SimpleCppStructGeneratesCorrectCSharpStruct),
                Assembly = nameof(SimpleCppStructGeneratesCorrectCSharpStruct),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile(testDirectory, "simpleStruct", @"
                        struct Test {
                            int field;
                        };
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };
            var result = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(result.exitCode, result.output);

            var compilation = GetCompilationForGeneratedCode(testDirectory);
            var structType = compilation.GetTypeByMetadataName($"{nameof(SimpleCppStructGeneratesCorrectCSharpStruct)}.Test");
            var attributes = structType.GetAttributes();
            var structLayoutAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Name == nameof(StructLayoutAttribute));
            Assert.NotNull(structLayoutAttribute);
            Assert.Equal((int)LayoutKind.Sequential, structLayoutAttribute.ConstructorArguments.First().Value);
            var fieldMember = structType.GetMembers("Field").Single();
            Assert.Equal(SymbolKind.Field, fieldMember.Kind);
            var field = (IFieldSymbol)fieldMember;
            Assert.Equal(compilation.GetTypeByMetadataName("System.Int32"), field.Type);
        }

        [Fact]
        public void StructWithMultipleMembersGeneratesStructWithMembersInCorrectOrder()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(StructWithMultipleMembersGeneratesStructWithMembersInCorrectOrder),
                Assembly = nameof(StructWithMultipleMembersGeneratesStructWithMembersInCorrectOrder),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile(testDirectory, "structWithMultipleMembers", @"
                        struct Test {
                            int field1;
                            int field2;
                        };
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var result = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(result.exitCode, result.output);

            var compilation = GetCompilationForGeneratedCode(testDirectory);
            var structType = compilation.GetTypeByMetadataName($"{nameof(StructWithMultipleMembersGeneratesStructWithMembersInCorrectOrder)}.Test");
            var attributes = structType.GetAttributes();
            var structLayoutAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Name == nameof(StructLayoutAttribute));
            Assert.NotNull(structLayoutAttribute);
            Assert.Equal((int)LayoutKind.Sequential, structLayoutAttribute.ConstructorArguments.First().Value);
            var members = structType.GetMembers().ToArray();
            Assert.Equal("Field1", members[0].Name);
            Assert.Equal("Field2", members[1].Name);
        }
    }
}
