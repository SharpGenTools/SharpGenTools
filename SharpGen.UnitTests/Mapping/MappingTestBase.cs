using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public abstract class MappingTestBase : TestBase
    {
        protected MappingTestBase(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        protected (CsAssembly Assembly, IEnumerable<DefineExtensionRule> Defines) MapModel(CppModule module, ConfigFile config)
        {
            var transformer = CreateTransformer();

            return transformer.Transform(module, ConfigFile.Load(config, new string[0], Logger), null);
        }

        protected (IEnumerable<BindRule> bindings, IEnumerable<DefineExtensionRule> defines) GetConsumerBindings(CppModule module, ConfigFile config)
        {
            var transformer = CreateTransformer();

            transformer.Transform(module, ConfigFile.Load(config, new string[0], Logger), null);

            return transformer.GenerateTypeBindingsForConsumers();
        }

        private TransformManager CreateTransformer()
        {
            var docLinker = new DocumentationLinker();
            var typeRegistry = new TypeRegistry(Logger, docLinker);
            var namingRules = new NamingRulesManager();

            // Run the main mapping process
            var transformer = new TransformManager(
                new GlobalNamespaceProvider("SharpGen.Runtime"),
                namingRules,
                Logger,
                typeRegistry,
                docLinker,
                new ConstantManager(namingRules, docLinker))
            {
                ForceGenerator = true
            };
            return transformer;
        }
    }
}
