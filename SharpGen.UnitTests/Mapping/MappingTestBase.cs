using System;
using System.Collections.Generic;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping;

public abstract class MappingTestBase : TestBase
{
    protected MappingTestBase(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    protected (CsAssembly Assembly, IEnumerable<DefineExtensionRule> Defines) MapModel(CppModule module, ConfigFile config)
    {
        var transformer = CreateTransformer();
        config.Load(null, Array.Empty<string>(), Logger);
        return transformer.Transform(module, config);
    }

    protected (IEnumerable<BindRule> bindings, IEnumerable<DefineExtensionRule> defines) GetConsumerBindings(CppModule module, ConfigFile config)
    {
        var transformer = CreateTransformer();
        config.Load(null, Array.Empty<string>(), Logger);
        transformer.Transform(module, config);

        return transformer.GenerateTypeBindingsForConsumers();
    }

    private TransformManager CreateTransformer()
    {
        NamingRulesManager namingRules = new();

        // Run the main mapping process
        return new TransformManager(
            namingRules,
            new ConstantManager(namingRules, Ioc),
            Ioc
        );
    }
}