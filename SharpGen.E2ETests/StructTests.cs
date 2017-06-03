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
            Assert.Equal(0, result.exitCode);

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
    }
}
