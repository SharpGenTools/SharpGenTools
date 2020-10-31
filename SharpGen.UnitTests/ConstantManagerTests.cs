using System;
using FakeItEasy;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;

namespace SharpGen.UnitTests
{
    public class ConstantManagerTests
    {
        [Fact]
        public void CanAddConstantFromCppConstant()
        {
            var constantManager = new ConstantManager(new NamingRulesManager(), A.Fake<IDocumentationLinker>());
            var value = "1";
            var macro = new CppConstant("Macro", value);
            var csTypeName = "MyClass";
            var constantName = "Constant";
            constantManager.AddConstantFromMacroToCSharpType(new CppElementFinder(macro), "Macro", csTypeName, "int", constantName, "$1", null, "namespace");
            var csStructure = new CsStruct
            {
                Name = csTypeName
            };
            constantManager.AttachConstants(csStructure);

            Assert.Single(csStructure.Items);
            var variable = (CsVariable)csStructure.Items[0];

            Assert.Equal(value, variable.Value);
            Assert.Equal(constantName, variable.Name);

        }

        [Fact]
        public void CanAddConstantFromGuid()
        {
            var constantManager = new ConstantManager(new NamingRulesManager(), A.Fake<IDocumentationLinker>());
            var guid = Guid.NewGuid();
            var macro = new CppGuid
            {
                Name = "Macro",
                Guid = guid
            };
            var csTypeName = "MyClass";
            var constantName = "Constant";
            constantManager.AddConstantFromMacroToCSharpType(new CppElementFinder(macro), "Macro", csTypeName, "int", constantName, "$1", null, "namespace");
            var csStructure = new CsStruct
            {
                Name = csTypeName
            };
            constantManager.AttachConstants(csStructure);

            Assert.Single(csStructure.Items);
            var variable = (CsVariable)csStructure.Items[0];

            Assert.Equal(guid.ToString(), variable.Value.ToString());
            Assert.Equal(constantName, variable.Name);

        }
    }
}
