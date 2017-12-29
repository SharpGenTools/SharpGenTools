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

        [Fact]
        public void StructWithBoolToIntMemberGeneratesBoolField()
        {
            var config = new Config.ConfigFile
            {
                Namespace = nameof(StructWithBoolToIntMemberGeneratesBoolField),
                Assembly = nameof(StructWithBoolToIntMemberGeneratesBoolField),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("boolToInt", @"
                        struct BoolToInt {
                            int test;
                        };
                    ")
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
                        Field = "BoolToInt::test",
                        MappingType = "bool",
                    }
                }
            };

            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(StructWithBoolToIntMemberGeneratesBoolField)}.BoolToInt");
            var member = structType.GetMembers("Test")[0] as IFieldSymbol;
            Assert.NotNull(member);
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Boolean), member.Type);
        }

        [Fact]
        public void StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty()
        {
            var config = new Config.ConfigFile
            {
                Namespace = nameof(StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty),
                Assembly = nameof(StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty),
                IncludeDirs = { GetTestFileIncludeRule() },
                Includes =
                {
                    CreateCppFile("boolToInt", @"
                        struct BoolToInt {
                            int test[3];
                        };

                        extern ""C"" bool TestFunction(BoolToInt t);
                    ")
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32"),
                    new Config.BindRule("bool", "System.Boolean")
                },
                Extension =
                {
                    new Config.CreateExtensionRule
                    {
                        NewClass = $"{nameof(StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty)}.Functions",
                        Visibility = Config.Visibility.Public
                    }
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Field = "BoolToInt::test",
                        MappingType = "bool",
                    },
                    new Config.MappingRule
                    {
                        Function = "TestFunction",
                        CsClass = $"{nameof(StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty)}.Functions",
                        FunctionDllName="Dll"
                    }
                }
            };

            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(StructWithBoolToIntArrayMemberGeneratesBoolArrayProperty)}.BoolToInt");
            var member = structType.GetMembers("Test")[0] as IPropertySymbol;
            Assert.NotNull(member);
            Assert.Equal(TypeKind.Array, member.Type.TypeKind);
            var memberType = (IArrayTypeSymbol)member.Type;
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Boolean), memberType.ElementType);
            var marshalType = structType.GetTypeMembers("__Native")[0];
            Assert.NotNull(marshalType);
        }
    }
}
