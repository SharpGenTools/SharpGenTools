using System;
using System.Linq;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests;

public class ConstantManagerTests : TestBase
{
    private readonly ConstantManager constantManager;

    public ConstantManagerTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        constantManager = new ConstantManager(new NamingRulesManager(), Ioc);
    }

    [Fact]
    public void CanAddConstantFromCppConstant()
    {
        var value = "1";
        var macro = new CppConstant("Macro", "int", value);
        var csTypeName = "MyClass";
        var constantName = "Constant";
        constantManager.AddConstantFromMacroToCSharpType(new CppElementFinder(macro), "Macro", csTypeName, "int", constantName, "$1", null, "namespace", false);
        var csStructure = new CsStruct(null, csTypeName);
        constantManager.AttachConstants(csStructure);

        Assert.Single(csStructure.Items);
        var variable = (CsExpressionConstant)csStructure.Items.First();

        Assert.Equal(value, variable.Value);
        Assert.Equal(constantName, variable.Name);
    }

    [Fact]
    public void CanAddConstantFromGuid()
    {
        var guid = Guid.NewGuid();
        var macro = new CppGuid("Macro", guid);
        var csTypeName = "MyClass";
        var constantName = "Constant";
        constantManager.AddConstantFromMacroToCSharpType(new CppElementFinder(macro), "Macro", csTypeName, "System.Guid", constantName, "$1", null, "namespace", false);
        var csStructure = new CsStruct(null, csTypeName);
        constantManager.AttachConstants(csStructure);

        Assert.Single(csStructure.Items);
        var variable = (CsGuidConstant)csStructure.Items.First();

        Assert.Equal(guid, variable.Value);
        Assert.Equal(constantName, variable.Name);
    }

    [Fact]
    public void CanAddConstantFromGuidAndFallback()
    {
        var guid = Guid.NewGuid();
        var macro = new CppGuid("Macro", guid);
        var csTypeName = "MyClass";
        var constantName = "Constant";
        constantManager.AddConstantFromMacroToCSharpType(new CppElementFinder(macro), "Macro", csTypeName, "int", constantName, "$1", null, "namespace", false);
        var csStructure = new CsStruct(null, csTypeName);
        constantManager.AttachConstants(csStructure);

        Assert.Single(csStructure.Items);
        var variable = (CsExpressionConstant)csStructure.Items.First();

        Assert.Equal(guid.ToString(), variable.Value);
        Assert.Equal(constantName, variable.Name);
    }
}