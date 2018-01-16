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
    public abstract class MappingTestBase
    {
        private readonly ITestOutputHelper outputHelper;

        protected MappingTestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        protected (CsSolution Solution, IEnumerable<DefineExtensionRule> Defines) MapModel(CppModule module, ConfigFile config, bool failTestOnError = true)
        {
            var logger = new Logger(new XUnitLogger(outputHelper, failTestOnError), null);

            var typeRegistry = new TypeRegistry(logger);
            var namingRules = new NamingRulesManager();
            var docAggregator = new DocumentationLinker(typeRegistry);

            // Run the main mapping process
            var transformer = new TransformManager(
                new GlobalNamespaceProvider("SharpGen.Runtime"),
                namingRules,
                logger,
                typeRegistry,
                docAggregator,
                new ConstantManager(namingRules, typeRegistry),
                new AssemblyManager())
            {
                ForceGenerator = true
            };

            return transformer.Transform(module, ConfigFile.Load(config, new string[0], logger), null);
        }
    }
}
