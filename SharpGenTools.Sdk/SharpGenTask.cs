using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGenTools.Sdk
{
    public class SharpGenTask : Task
    {
        [Required]
        public ITaskItem[] MappingFiles { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles { get; set; }

        [Required]
        public string CastXmlPath { get; set; }

        [Required]
        public string AppType { get; set; }

        [Required]
        public string GlobalNamespace { get; set; }
        
        [Required]
        public bool GenerateDocs { get; set; }

        [Required]
        public string VcToolsPath { get; set; }

        public override bool Execute()
        {
            if (MappingFiles.Length == 0)
            {
                return true;
            }
            if (MappingFiles.Length != 1)
            {
                Log.LogError("Only one root MappingFile is supported.");
                return false;
            }

            try
            {
                var codeGenApp = new CodeGenApp(new SharpGen.Logging.Logger(new MsBuildLogger(Log), null))
                {
                    CastXmlExecutablePath = CastXmlPath,
                    AppType = AppType,
                    ConfigRootPath = MappingFiles[0].ItemSpec,
                    GlobalNamespace = new GlobalNamespaceProvider(GlobalNamespace),
                    IsGeneratingDoc = GenerateDocs,
                    VcToolsPath = VcToolsPath
                };

                if(!codeGenApp.Init())
                {
                    return false;
                }
                codeGenApp.Run();
                // TODO: Get output items
                return true;
            }
            catch (CodeGenFailedException)
            {
                return false;
            }
        }
    }
}
