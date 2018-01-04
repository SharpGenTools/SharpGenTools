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

        public bool ForceParsing { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var cppHeaderGenerator = new CppHeaderGenerator(
                SharpGenLogger,
                ForceParsing,
                OutputPath);

            var configsWithHeaders = new HashSet<ConfigFile>();
            foreach (var cfg in config.ConfigFilesLoaded)
            {
                if (HeaderFiles.Any(item => item.GetMetadata("ConfigId") == cfg.Id))
                {
                    configsWithHeaders.Add(cfg);
                }
            }

            var configsWithExtensions = new HashSet<string>();
            foreach (var file in ExtensionHeaders)
            {
                configsWithExtensions.Add(file.GetMetadata("ConfigId"));
            }

            var (updatedConfigs, consumerCppConfig) = cppHeaderGenerator.GenerateCppHeaders(config, configsWithHeaders, configsWithExtensions);

            consumerCppConfig.Id = "ConsumerCppConfigCache";

            consumerCppConfig.Write(CppConsumerConfigCache.ItemSpec);

            var updatedConfigFiles = new List<ITaskItem>();

            foreach (var cfg in configsWithHeaders)
            {
                if (updatedConfigs.Contains(cfg))
                {
                    updatedConfigFiles.Add(new TaskItem(cfg.Id));
                }
            }

            UpdatedConfigs = updatedConfigFiles.ToArray();

            return true;
        }
    }
}
