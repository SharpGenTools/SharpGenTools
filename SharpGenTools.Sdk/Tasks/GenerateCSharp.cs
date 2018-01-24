using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Generator;
using System;
using System.Collections.Generic;
using System.Text;
using SharpGen;
using Logger = SharpGen.Logging.Logger;
using SharpGen.Model;
using System.Xml;
using System.IO;
using SharpGen.Transform;
using System.Linq;

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

        public ITaskItem[] ExternalDocumentation { get; set; }

        public override bool Execute()
        {
            var documentationFiles = new Dictionary<string, XmlDocument>();

            foreach (var file in ExternalDocumentation ?? Enumerable.Empty<ITaskItem>())
            {
                using (var stream = File.OpenRead(file.ItemSpec))
                {
                    var xml = new XmlDocument();
                    xml.Load(stream);
                    documentationFiles.Add(file.ItemSpec, xml);
                }
            }

            var generator = new RoslynGenerator(
                new Logger(new MsBuildSharpGenLogger(Log), null),
                new GlobalNamespaceProvider(GlobalNamespace),
                new CachedDocumentationLinker(DocLinkCache.ItemSpec),
                new ExternalDocCommentsReader(documentationFiles));

            generator.Run(CsSolution.Read(Model.ItemSpec), OutputDirectory, GeneratedCodeFolder, IncludeAssemblyNameFolder);

            return true;
        }
    }
}
