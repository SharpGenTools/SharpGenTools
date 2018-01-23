using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class Remove : MappingTestBase
    {
        public Remove(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Enum()
        {
            var config = new Config.ConfigFile
            {
                Id = nameof(Enum),
                Assembly = nameof(Enum),
                Namespace = nameof(Enum),
                Includes =
                {
                    new Config.IncludeRule
                    {
                        Attach = true,
                        File = "cppEnum.h",
                        Namespace = nameof(Enum)
                    }
                },
                Mappings =
                {
                    new Config.RemoveRule
                    {
                        Enum = @".*ToRemove\d+"
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
            cppInclude.Add(cppEnum);
            cppInclude.Add(new CppEnum
            {
                Name = "EnumToRemove1"
            });
            cppModel.Add(cppInclude);

            var (solution, _) = MapModel(cppModel, config);

            var members = solution.EnumerateDescendants();

            Assert.Single(members.OfType<CsEnum>());
        }
    }
}
