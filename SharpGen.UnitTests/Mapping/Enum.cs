using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
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

            var cppModel = new CppModule("SharpGenTestModule");

            var cppInclude = new CppInclude("cppEnum");

            var cppEnum = new CppEnum("TestEnum");

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
            Assert.Equal(TypeRegistry.Int32, csEnum.UnderlyingType);
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

            var cppModel = new CppModule("SharpGenTestModule");

            var cppInclude = new CppInclude("cppEnum");

            var cppEnum = new CppEnum("TestEnum");

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
            Assert.Equal(TypeRegistry.Int32, csEnum.UnderlyingType);
        }

        [Theory]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("int")]
        [InlineData("ushort")]
        [InlineData("uint")]
        public void ExplicitUnderlyingType(string underlyingType)
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

            var cppModel = new CppModule("SharpGenTestModule");

            var cppInclude = new CppInclude("cppEnum");

            var cppEnum = new CppEnum("TestEnum")
            {
                UnderlyingType = underlyingType
            };

            cppInclude.Add(cppEnum);
            cppModel.Add(cppInclude);

            var (solution, _) = MapModel(cppModel, config);

            Assert.Single(solution.EnumerateDescendants<CsEnum>());

            var csEnum = solution.EnumerateDescendants<CsEnum>().First();
            Assert.Equal(TypeRegistry.ImportPrimitiveType(underlyingType), csEnum.UnderlyingType);
        }
    }
}
