using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Frameworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpGenTools.Sdk
{
    public class ResolveBestBindMapping : Task
    {
        [Required]
        public ITaskItem[] UnresolvedPackageBindMappings { get; set; }

        [Required]
        public string TargetFramework { get; set; }

        [Output]
        public ITaskItem[] ResolvedPackageBindMappings { get; set; }

        public override bool Execute()
        {
            var frameworkReducer = new FrameworkReducer();
            var bestMappingFiles = new List<ITaskItem>();
            foreach (var file in UnresolvedPackageBindMappings)
            {
                var bestMapping = frameworkReducer.GetNearest(NuGetFramework.Parse(TargetFramework),
                    file.GetMetadata("TargetFrameworks").Split(';')
                        .Select(framework => NuGetFramework.Parse(framework)));
                var pathToBestMapping = Path.Combine(file.GetMetadata("BasePath"), bestMapping.GetShortFolderName(), "SharpGen", file.ItemSpec + ".BindMapping.xml");
                bestMappingFiles.Add(new TaskItem(pathToBestMapping));
            }
            ResolvedPackageBindMappings = bestMappingFiles.ToArray();
            return true;
        }
    }
}
