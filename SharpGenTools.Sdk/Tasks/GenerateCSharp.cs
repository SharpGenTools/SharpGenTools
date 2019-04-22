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

        public ITaskItem[] GlobalNamespaceOverrides { get; set; }

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

            var globalNamespace = new GlobalNamespaceProvider(GlobalNamespace);

            foreach (var nameOverride in GlobalNamespaceOverrides ?? Enumerable.Empty<ITaskItem>())
            {
                var wellKnownName = nameOverride.ItemSpec;
                var overridenName = nameOverride.GetMetadata("Override");
                if (overridenName != null && Enum.TryParse(wellKnownName, out WellKnownName name))
                {
                    globalNamespace.OverrideName(name, overridenName);
                }
            }

            var generator = new RoslynGenerator(
                new Logger(new MsBuildSharpGenLogger(Log), null),
                globalNamespace,
                new CachedDocumentationLinker(DocLinkCache.ItemSpec),
                new ExternalDocCommentsReader(documentationFiles));

            generator.Run(CsAssembly.Read(Model.ItemSpec), OutputDirectory, GeneratedCodeFolder);

            return true;
        }
    }
}
