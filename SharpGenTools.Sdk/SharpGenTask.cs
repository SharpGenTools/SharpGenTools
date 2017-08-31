using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpGenTools.Sdk
{
    public class SharpGenTask : Task
    {
        [Required]
        public ITaskItem[] MappingFiles { get; set; }

        [Required]
        public string CastXmlPath { get; set; }

        [Required]
        public string[] AppTypes { get; set; }

        [Required]
        public string GlobalNamespace { get; set; }

        [Required]
        public bool GenerateDocs { get; set; }

        [Required]
        public string VcToolsPath { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        public string GeneratedCodeFolder { get; private set; }
        
        [Required]
        public bool IncludeAssemblyNameFolder { get; private set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            try
            {
                foreach (var mappingFile in MappingFiles)
                {
                    foreach (var appType in AppTypes)
                    {
                        var codeGenApp = new CodeGenApp(new SharpGen.Logging.Logger(new MsBuildLogger(Log), null))
                        {
                            CastXmlExecutablePath = CastXmlPath,
                            AppType = appType,
                            ConfigRootPath = mappingFile.ItemSpec,
                            GlobalNamespace = new GlobalNamespaceProvider(GlobalNamespace),
                            IsGeneratingDoc = GenerateDocs,
                            VcToolsPath = VcToolsPath,
                            IntermediateOutputPath = IntermediateOutputPath,
                            IncludeAssemblyNameFolder = IncludeAssemblyNameFolder,
                            GeneratedCodeFolder = GeneratedCodeFolder
                        };

                        if(!codeGenApp.Init())
                        {
                        }
                        codeGenApp.Run();
                    }
                }
                return true;
            }
            catch (CodeGenFailedException)
            {
                return false;
            }
        }
    }
}
