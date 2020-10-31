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
using SharpGen.Logging;

namespace SharpGenTools.Sdk.Tasks
{
    public class GenerateCSharp : Task
    {
        [Required]
        public ITaskItem Model { get; set; }

        [Required]
        public string GeneratedCodeFolder { get; set; }

        [Required]
        public ITaskItem DocLinkCache { get; set; }

        public ITaskItem[] Platforms { get; set; }

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

            var globalNamespace = new GlobalNamespaceProvider();

            foreach (var nameOverride in GlobalNamespaceOverrides ?? Enumerable.Empty<ITaskItem>())
            {
                var wellKnownName = nameOverride.ItemSpec;
                var overridenName = nameOverride.GetMetadata("Override");
                if (overridenName != null && Enum.TryParse(wellKnownName, out WellKnownName name))
                {
                    globalNamespace.OverrideName(name, overridenName);
                }
            }

            PlatformDetectionType platformMask = 0;

            foreach (var platform in Platforms ?? Enumerable.Empty<ITaskItem>())
            {
                if (!Enum.TryParse<PlatformDetectionType>("Is" + platform.ItemSpec, out var parsedPlatform))
                {
                    Log.LogWarning(null, LoggingCodes.InvalidPlatformDetectionType, null, null, 0, 0, 0, 0, $"The platform type {platform} is an unknown platform to SharpGenTools. Falling back to Any platform detection.");
                    platformMask = PlatformDetectionType.Any;
                }
                else
                {
                    platformMask |= parsedPlatform;
                }
            }

            if (platformMask == 0)
            {
                platformMask = PlatformDetectionType.Any;
            }

            var config = new GeneratorConfig
            {
                Platforms = platformMask
            };

            var generator = new RoslynGenerator(
                new Logger(new MSBuildSharpGenLogger(Log), null),
                globalNamespace,
                new CachedDocumentationLinker(DocLinkCache.ItemSpec),
                new ExternalDocCommentsReader(documentationFiles),
                config);

            generator.Run(CsAssembly.Read(Model.ItemSpec), GeneratedCodeFolder);

            return true;
        }
    }
}
