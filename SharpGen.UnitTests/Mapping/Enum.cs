using System;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class Enum : MappingTestBase
    {
        public Enum(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Basic()
        {
            var config = new ConfigFile
            {
                Id = nameof(Basic),
                Namespace = nameof(Basic),
                Includes =
                {
                    new IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(Basic)
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
            Assert.Equal(typeof(int), csEnum.UnderlyingType.Type);
        }

        [Fact]
        public void ExplicitValues()
        {
            var config = new ConfigFile
            {
                Id = nameof(ExplicitValues),
                Namespace = nameof(ExplicitValues),
                Includes =
                {
                    new IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(ExplicitValues)
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
            Assert.Single(csEnum.EnumItems.Where(item => item.Name == "Element3" && item.Value == "10"));
            Assert.Equal(typeof(int), csEnum.UnderlyingType.Type);
        }

        [Theory]
        [InlineData(typeof(short), "short")]
        [InlineData(typeof(int), "int")]
        [InlineData(typeof(ushort), "ushort")]
        [InlineData(typeof(uint), "uint")]
        public void ExplicitUnderlyingType(Type underlyingType, string underlyingTypeShortName)
        {
            var config = new ConfigFile
            {
                Id = nameof(ExplicitUnderlyingType),
                Namespace = nameof(ExplicitUnderlyingType),
                Includes =
                {
                    new IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(ExplicitUnderlyingType)
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
                Name = "TestEnum",
                UnderlyingType = underlyingTypeShortName
            };

            cppInclude.Add(cppEnum);
            cppModel.Add(cppInclude);

            var (solution, _) = MapModel(cppModel, config);

            var members = solution.EnumerateDescendants();

            Assert.Single(members.OfType<CsEnum>());

            var csEnum = members.OfType<CsEnum>().First();
            Assert.Equal(underlyingType, csEnum.UnderlyingType.Type);
        }
    }
}
