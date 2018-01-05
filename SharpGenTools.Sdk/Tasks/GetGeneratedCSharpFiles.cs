using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Generator;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGenTools.Sdk.Tasks
{
    public class GetGeneratedCSharpFiles : Task
    {
        [Required]
        public ITaskItem Model { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string GeneratedCodeFolder { get; set; }

        public bool IncludeAssemblyNameFolder { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles { get; set; }

        public override bool Execute()
        {
            var solution = CsSolution.Read(Model.ItemSpec);
            
            var files = RoslynGenerator.GetFilePathsForGeneratedFiles(solution, OutputDirectory, GeneratedCodeFolder, IncludeAssemblyNameFolder);

            GeneratedFiles = files
                .SelectMany(assembly =>
                    assembly.Value.Select(file => (assembly: assembly.Key, file)))
                .Select(file =>
                {
                    var item = new TaskItem(file.file);
                    item.SetMetadata("Assembly", file.assembly);
                    return item;
                }).ToArray();
            return true;
        }
    }
}
