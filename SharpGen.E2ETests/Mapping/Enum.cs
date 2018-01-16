using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Mapping
{
    public class Enum : MappingTestBase
    {
        public Enum(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void BasicCppEnumMapsToCSharpEnum()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(BasicCppEnumMapsToCSharpEnum),
                Assembly = nameof(BasicCppEnumMapsToCSharpEnum),
                Namespace = nameof(BasicCppEnumMapsToCSharpEnum),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(BasicCppEnumMapsToCSharpEnum)
                    }
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

            cppEnum.AddEnumItem("Element1", "0");
            cppEnum.AddEnumItem("Element2", "1");
            cppInclude.Add(cppEnum);
            cppModel.Add(cppInclude);

            var (solution, _) = MapModel(cppModel, config);

            var members = solution.EnumerateDescendants();

            Assert.Single(members.OfType<CsEnum>());

            var csEnum = members.OfType<CsEnum>().First();

            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element1" && item.Value == "0"));
            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element2" && item.Value == "1"));
            Assert.Equal(typeof(int), csEnum.Type);
        }

        [Fact]
        public void CppEnumWithExplicitValuesMapsToCSharpEnumWithCorrectValue()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(BasicCppEnumMapsToCSharpEnum),
                Assembly = nameof(BasicCppEnumMapsToCSharpEnum),
                Namespace = nameof(BasicCppEnumMapsToCSharpEnum),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(BasicCppEnumMapsToCSharpEnum)
                    }
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

            cppEnum.AddEnumItem("Element1", "10");
            cppEnum.AddEnumItem("Element2", "15");
            cppEnum.AddEnumItem("Element3", "10");
            cppInclude.Add(cppEnum);
            cppModel.Add(cppInclude);

            var (solution, _) = MapModel(cppModel, config);

            var members = solution.EnumerateDescendants();

            Assert.Single(members.OfType<CsEnum>());

            var csEnum = members.OfType<CsEnum>().First();

            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element1" && item.Value == "10"));
            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element2" && item.Value == "15"));
            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element2" && item.Value == "10"));
            Assert.Equal(typeof(int), csEnum.Type);
        }
    }
}
