using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using SharpGen;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGenTools.Sdk.Tasks
{
    public class TransformCppToCSharp : SharpGenTaskBase
    {
        [Required]
        public string ConsumerBindMappingConfigId { get; set; }

        [Required]
        public ITaskItem CppModule { get; set; }

        [Required]
        public ITaskItem CppConsumerConfigCache { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public ITaskItem[] GlobalNamespaceOverrides { get; set; }

        [Required]
        public ITaskItem CSharpModel { get; set; }

        [Required]
        public ITaskItem DocLinksCache { get; set; }

        public bool GenerateConsumerConfig { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var group = SharpGen.CppModel.CppModule.Read(CppModule.ItemSpec);
            config.ExpandDynamicVariables(SharpGenLogger, group);

            var docLinker = new DocumentationLinker();
            var typeRegistry = new TypeRegistry(SharpGenLogger, docLinker);
            var namingRules = new NamingRulesManager();

            var globalNamespace = new GlobalNamespaceProvider();

            foreach (var nameOverride in GlobalNamespaceOverrides ?? Enumerable.Empty<ITaskItem>())
            {
                var wellKnownName = nameOverride.ItemSpec;
                var overridenName = nameOverride.GetMetadata("Override");
                if (overridenName != null && Enum.TryParse(wellKnownName, out WellKnownName name))
                {
                    globalNamespace.OverrideName(name, overridenName);
                }
            }

            // Run the main mapping process
            var transformer = new TransformManager(
                globalNamespace,
                namingRules,
                SharpGenLogger,
                typeRegistry,
                docLinker,
                new ConstantManager(namingRules, docLinker)
            );

            var (solution, defines) = transformer.Transform(group, config);

            solution.Write(CSharpModel.ItemSpec);

            var consumerConfig = ConfigFile.Load(CppConsumerConfigCache.ItemSpec, new string[0], SharpGenLogger);

            consumerConfig.Id = ConsumerBindMappingConfigId;

            consumerConfig.Extension = new List<ExtensionBaseRule>(defines);

            var (bindings, generatedDefines) = transformer.GenerateTypeBindingsForConsumers();

            consumerConfig.Bindings.AddRange(bindings);
            consumerConfig.Extension.AddRange(generatedDefines);

            consumerConfig.Mappings.AddRange(
                docLinker.GetAllDocLinks().Select(
                    link => new MappingRule
                    {
                        DocItem = link.cppName,
                        MappingNameFinal = link.cSharpName
                    }));

            if (GenerateConsumerConfig)
            {
                GenerateConfigForConsumers(consumerConfig); 
            }

            SaveDocLinks(docLinker);

            return !Log.HasLoggedErrors;
        }

        private void SaveDocLinks(DocumentationLinker docAggregator)
        {
            using (var file = File.Open(DocLinksCache.ItemSpec, FileMode.Create))
            using (var writer = new StreamWriter(file))
            {
                foreach (var (cppName, cSharpName) in docAggregator.GetAllDocLinks())
                {
                    writer.Write(cppName);
                    writer.Write(CachedDocumentationLinker.Delimiter);
                    writer.WriteLine(cSharpName);
                }
            }
        }

        private void GenerateConfigForConsumers(ConfigFile consumerConfig)
        {
            var consumerBindMappingFileName = Path.Combine(OutputPath, $"{ConsumerBindMappingConfigId}.BindMapping.xml");
            
            using (var consumerBindMapping = File.Create(consumerBindMappingFileName))
            {
                consumerConfig.Write(consumerBindMapping);
            }
        }
    }
}
