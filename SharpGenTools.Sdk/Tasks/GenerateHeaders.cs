using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Config;
using SharpGen.Parser;

namespace SharpGenTools.Sdk.Tasks
{
    public class GenerateHeaders : SharpGenCppTaskBase
    {
        [Required]
        public ITaskItem[] HeaderFiles { get; set; }

        [Required]
        public ITaskItem[] ExtensionHeaders { get; set; }

        [Required]
        public ITaskItem CppConsumerConfigCache { get; set; }

        [Output]
        public ITaskItem[] UpdatedConfigs { get; set; }

        [Required]
        public string OutputPath { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var cppHeaderGenerator = new CppHeaderGenerator(SharpGenLogger, OutputPath);

            var configsWithHeaders = new HashSet<ConfigFile>(ConfigFile.IdComparer);
            var configsWithExtensions = new HashSet<ConfigFile>(ConfigFile.IdComparer);

            foreach (var cfg in config.ConfigFilesLoaded)
            {
                if (HeaderFiles.Any(item => item.GetMetadata("ConfigId") == cfg.Id))
                    configsWithHeaders.Add(cfg);

                if (ExtensionHeaders.Any(item => item.GetMetadata("ConfigId") == cfg.Id))
                    configsWithExtensions.Add(cfg);
            }

            var cppHeaderGenerationResult = cppHeaderGenerator.GenerateCppHeaders(config, configsWithHeaders, configsWithExtensions);

            var consumerConfig = new ConfigFile
            {
                Id = "CppConsumerConfig",
                IncludeProlog = {cppHeaderGenerationResult.Prologue}
            };

            consumerConfig.Write(CppConsumerConfigCache.ItemSpec);

            var updatedConfigFiles = new List<ITaskItem>();

            foreach (var cfg in configsWithHeaders)
            {
                if (cppHeaderGenerationResult.UpdatedConfigs.Contains(cfg) && cfg.AbsoluteFilePath != null)
                {
                    var item = new TaskItem(cfg.AbsoluteFilePath);
                    item.SetMetadata("Id", cfg.Id);
                    updatedConfigFiles.Add(item);
                }
            }

            UpdatedConfigs = updatedConfigFiles.ToArray();

            return !SharpGenLogger.HasErrors;
        }
    }
}
