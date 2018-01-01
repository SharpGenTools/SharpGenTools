using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpGen.Config;
using System.Linq;

namespace SharpGenTools.Sdk
{
    public class SharpGen : Task
    {
        [Required]
        public ITaskItem[] MappingFiles { get; set; }

        [Required]
        public string CastXmlPath { get; set; }

        [Required]
        public string GlobalNamespace { get; set; }

        [Required]
        public bool GenerateDocs { get; set; }

        [Required]
        public string VcToolsPath { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        public string GeneratedCodeFolder { get; set; }
        
        [Required]
        public bool IncludeAssemblyNameFolder { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string ConsumerBindMappingConfigId { get; set; }

        public string[] Macros { get; set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            try
            {
                var config = new ConfigFile
                {
                    Files = MappingFiles.Select(file => file.ItemSpec).ToList(),
                    Id = "SharpGen-MSBuild"
                };

                RunCodeGen(config);
                return true;
            }
            catch (CodeGenFailedException)
            {
                return false;
            }
        }

        private void RunCodeGen(ConfigFile config)
        {
            var codeGenApp = new CodeGenApp(new global::SharpGen.Logging.Logger(new MsBuildSharpGenLogger(Log), null))
            {
                CastXmlExecutablePath = CastXmlPath,
                Config = config,
                GlobalNamespace = new GlobalNamespaceProvider(GlobalNamespace),
                IsGeneratingDoc = GenerateDocs,
                VcToolsPath = VcToolsPath,
                IntermediateOutputPath = IntermediateOutputPath,
                OutputDirectory = OutputDirectory,
                IncludeAssemblyNameFolder = IncludeAssemblyNameFolder,
                GeneratedCodeFolder = GeneratedCodeFolder,
                ConsumerBindMappingConfigId = ConsumerBindMappingConfigId
            };

            if (Macros != null)
            {
                foreach (var macro in Macros)
                {
                    codeGenApp.Macros.Add(macro);
                } 
            }

            if (!codeGenApp.Init())
            {
            }
            codeGenApp.Run();
        }
    }
}
