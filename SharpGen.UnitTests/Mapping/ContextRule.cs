using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class ContextRule : MappingTestBase
    {
        public ContextRule(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void ContextRuleLimitsWhereMappingRuleExecutes()
        {
            var config = new ConfigFile
            {
                Id = nameof(ContextRuleLimitsWhereMappingRuleExecutes),
                Namespace = nameof(ContextRuleLimitsWhereMappingRuleExecutes),
                Includes =
                {
                    new IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(ContextRuleLimitsWhereMappingRuleExecutes)
                    },
                    new IncludeRule
                    {
                        Attach = true,
                        File = "secondFile.h",
                        Namespace = nameof(ContextRuleLimitsWhereMappingRuleExecutes)
                    }
                },
                Mappings =
                {
                    new Config.ContextRule("cppEnum"),
                    new MappingRule
                    {
                        Enum = "AnotherEnum",
                        MappingNameFinal = "NewEnum"
                    },
                    new ClearContextRule()
                }
            };

            var cppModel = new CppModule();

            var cppInclude = new CppInclude
            {
                Name = "cppEnum"
            };

            var cppEnum = new CppEnum
            {
                Name = "TestEnum"
            };

            var secondInclude = new CppInclude
            {
                Name = "secondFile"
            };

            var cppEnum2 = new CppEnum
            {
                Name = "AnotherEnum"
            };

            cppEnum.AddEnumItem("Element1", "0");
            cppEnum.AddEnumItem("Element2", "1");
            cppInclude.Add(cppEnum);
            cppModel.Add(cppInclude);


            var (solution, _) = MapModel(cppModel, config);

            Assert.Empty(solution.EnumerateDescendants().OfType<CsEnum>().Where(csEnum => csEnum.Name == "AnotherEnum"));
        }
    }
}
