using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using SharpGen;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGenTools.Sdk.Tasks
{
    class TransformCppToCSharp : SharpGenTaskBase
    {
        [Required]
        public string ConsumerBindMappingConfigId { get; set; }

        [Required]
        public ITaskItem FullCppModule { get; set; }

        [Required]
        public ITaskItem CppConsumerConfigCache { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public bool ForceGenerator { get; set; }

        [Required]
        public string GlobalNamespace { get; set; }

        [Required]
        public ITaskItem CSharpModel { get; set; }

        [Required]
        public ITaskItem DocLinksCache { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var group = CppModule.Read(FullCppModule.ItemSpec);
            config.ExpandDynamicVariables(SharpGenLogger, group);

            var typeRegistry = new TypeRegistry(SharpGenLogger);
            var namingRules = new NamingRulesManager();
            var docAggregator = new DocumentationLinker(typeRegistry);

            // Run the main mapping process
            var transformer = new TransformManager(
                new GlobalNamespaceProvider(GlobalNamespace),
                namingRules,
                SharpGenLogger,
                typeRegistry,
                docAggregator,
                new ConstantManager(namingRules, typeRegistry),
                new AssemblyManager())
            {
                ForceGenerator = ForceGenerator
            };

            var (solution, defines) = transformer.Transform(group, config, OutputPath);

            solution.Write(CSharpModel.ItemSpec);

            var consumerConfig = ConfigFile.Load(CppConsumerConfigCache.ItemSpec, new string[0], SharpGenLogger);

            consumerConfig.Id = ConsumerBindMappingConfigId;

            consumerConfig.Extension = new List<ConfigBaseRule>(defines);

            var (bindings, generatedDefines) = transformer.GenerateTypeBindingsForConsumers();

            consumerConfig.Bindings.AddRange(bindings);
            consumerConfig.Extension.AddRange(generatedDefines);

            GenerateConfigForConsumers(consumerConfig);

            SaveDocLinks(docAggregator);

            return true;
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
