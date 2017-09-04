using Microsoft.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class StructTests : TestBase
    {
        public StructTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

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
                    CreateCppFile("simpleStruct", @"
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
            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
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
                    CreateCppFile("structWithMultipleMembers", @"
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

            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(StructWithMultipleMembersGeneratesStructWithMembersInCorrectOrder)}.Test");
            var attributes = structType.GetAttributes();
            var structLayoutAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Name == nameof(StructLayoutAttribute));
            Assert.NotNull(structLayoutAttribute);
            Assert.Equal((int)LayoutKind.Sequential, structLayoutAttribute.ConstructorArguments.First().Value);
            var members = structType.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Field).ToArray();
            Assert.Equal("Field1", members[0].Name);
            Assert.Equal("Field2", members[1].Name);
        }

        [Fact]
        public void InheritingStructPutsItsMembersAfterBaseMembers()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(InheritingStructPutsItsMembersAfterBaseMembers),
                Assembly = nameof(InheritingStructPutsItsMembersAfterBaseMembers),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("inheritingStructs", @"
                        struct Base {
                            int field1;
                            int field2;
                        };
                        struct Inherited : Base {
                            int field3;
                        };
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32")
                }
            };

            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(InheritingStructPutsItsMembersAfterBaseMembers)}.Inherited");
            var attributes = structType.GetAttributes();
            var structLayoutAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Name == nameof(StructLayoutAttribute));
            Assert.NotNull(structLayoutAttribute);
            Assert.Equal((int)LayoutKind.Sequential, structLayoutAttribute.ConstructorArguments.First().Value);
            var members = structType.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Field).ToArray();
            Assert.Equal("Field1", members[0].Name);
            Assert.Equal("Field2", members[1].Name);
            Assert.Equal("Field3", members[2].Name);
        }
    }
}
