using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Mapping
{
    public abstract class MappingTestBase : TestBase
    {
        protected MappingTestBase(ITestOutputHelper outputHelper)
            :base(outputHelper)
        {
        }

        protected (CsSolution Solution, IEnumerable<DefineExtensionRule> Defines) MapModel(CppModule module, ConfigFile config)
        {
            var typeRegistry = new TypeRegistry(Logger);
            var namingRules = new NamingRulesManager();
            var docAggregator = new DocumentationLinker(typeRegistry);

            // Run the main mapping process
            var transformer = new TransformManager(
                new GlobalNamespaceProvider("SharpGen.Runtime"),
                namingRules,
                Logger,
                typeRegistry,
                docAggregator,
                new ConstantManager(namingRules, typeRegistry),
                new AssemblyManager())
            {
                ForceGenerator = true
            };

            return transformer.Transform(module, ConfigFile.Load(config, new string[0], Logger), null);
        }
    }
}
