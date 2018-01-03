using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Generator;
using System;
using System.Collections.Generic;
using System.Text;
using SharpGen;
using Logger = SharpGen.Logging.Logger;
using SharpGen.Model;

namespace SharpGenTools.Sdk.Tasks
{
    public class GenerateCSharp : Task
    {
        [Required]
        public ITaskItem Model { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string GeneratedCodeFolder { get; set; }

        public bool IncludeAssemblyNameFolder { get; set; }

        [Required]
        public ITaskItem DocLinkCache { get; set; }

        [Required]
        public string GlobalNamespace { get; set; }

        public override bool Execute()
        {
            var generator = new RoslynGenerator(
                new Logger(new MsBuildSharpGenLogger(Log), null),
                new GlobalNamespaceProvider(GlobalNamespace),
                new CachedDocumentationLinker(DocLinkCache.ItemSpec));

            generator.Run(CsSolution.Read(Model.ItemSpec), OutputDirectory, GeneratedCodeFolder, IncludeAssemblyNameFolder);

            return true;
        }
    }
}
