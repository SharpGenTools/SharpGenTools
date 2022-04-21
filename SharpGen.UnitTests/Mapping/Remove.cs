using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping;

public class Remove : MappingTestBase
{
    public Remove(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Enum()
    {
        var config = new ConfigFile
        {
            Id = nameof(Enum),
            Namespace = nameof(Enum),
            Includes =
            {
                new IncludeRule
                {
                    Attach = true,
                    File = "cppEnum.h",
                    Namespace = nameof(Enum)
                }
            },
            Mappings =
            {
                new RemoveRule
                {
                    Enum = @".*ToRemove\d+"
                }
            }
        };

        var cppModel = new CppModule("SharpGenTestModule");

        var cppInclude = new CppInclude("cppEnum");

        var cppEnum = new CppEnum("TestEnum");
        cppInclude.Add(cppEnum);
        cppInclude.Add(new CppEnum("EnumToRemove1"));
        cppModel.Add(cppInclude);

        var (solution, _) = MapModel(cppModel, config);

        var members = solution.EnumerateDescendants();

        Assert.Single(members.OfType<CsEnum>());
    }

    [Fact]
    public void RemoveParentDoesNotRemoveAllParents()
    {
        var config = new ConfigFile
        {
            Id = nameof(RemoveParentDoesNotRemoveAllParents),
            Namespace = nameof(RemoveParentDoesNotRemoveAllParents),
            Includes =
            {
                new IncludeRule
                {
                    Attach = true,
                    File = "cppEnum.h",
                    Namespace = nameof(RemoveParentDoesNotRemoveAllParents)
                }
            },
            Mappings =
            {
                new RemoveRule
                {
                    Method = @"#.*ToRemove"
                }
            }
        };

        var cppModel = new CppModule("SharpGenTestModule");

        var cppInclude = new CppInclude("cppEnum");

        var cppIface = new CppInterface("TestInterface");
        cppInclude.Add(cppIface);

        var cppMethod = new CppMethod("Method");
        cppMethod.Add(new CppParameter("ParamToRemove"));
        cppModel.Add(cppInclude);

        var (solution, _) = MapModel(cppModel, config);

        var members = solution.EnumerateDescendants();

        Assert.NotEmpty(members.OfType<CsInterface>());
        Assert.Empty(members.OfType<CsParameter>());
    }
}