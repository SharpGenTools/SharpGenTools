using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class RenameTests : TestBase
    {
        public RenameTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void MappingNameRuleRenamesStruct()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(MappingNameRuleRenamesStruct),
                Assembly = nameof(MappingNameRuleRenamesStruct),
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
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Struct = "Test",
                        MappingName = "MyStruct"
                    }
                }
            };
            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(MappingNameRuleRenamesStruct)}.MyStruct");
            Assert.NotNull(structType);
        }

        [Fact]
        public void MappingNameRuleRenamesStructMember()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(MappingNameRuleRenamesStructMember),
                Assembly = nameof(MappingNameRuleRenamesStructMember),
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
                },
                Mappings =
                {
                    new Config.MappingRule
                    {
                        Field = "Test::field",
                        MappingName = "MyField"
                    }
                }
            };
            var result = RunWithConfig(config);
            AssertRanSuccessfully(result.success, result.output);

            var compilation = GetCompilationForGeneratedCode();
            var structType = compilation.GetTypeByMetadataName($"{nameof(MappingNameRuleRenamesStructMember)}.Test");
            Assert.Equal(1, structType.GetMembers("MyField").Length);
        }
    }
}
