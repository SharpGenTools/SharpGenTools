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

        [Output]
        public ITaskItem[] GeneratedFiles { get; set; }

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

        public override bool Execute()
        {
            List<ITaskItem> outputItems = new List<ITaskItem>();
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
                            IntermediateOutputPath = IntermediateOutputPath
                        };

                        if(!codeGenApp.Init())
                        {
                            return false;
                        }
                        codeGenApp.Run();
                        outputItems.Add(new TaskItem(Path.Combine(IntermediateOutputPath, $"{mappingFile.GetMetadata("Filename")}-{appType}.check")));
                        outputItems.Add(new TaskItem(Path.Combine(IntermediateOutputPath, $"{mappingFile.GetMetadata("Filename")}-{appType}-CodeGen.check")));
                    }
                }
                return true;
            }
            catch (CodeGenFailedException)
            {
                return false;
            }
            finally
            {
                GeneratedFiles = outputItems.ToArray();
            }
        }
    }
}
