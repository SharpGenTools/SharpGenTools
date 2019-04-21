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

        [Output]
        public ITaskItem[] GeneratedFiles { get; set; }

        public override bool Execute()
        {
            var asm = CsAssembly.Read(Model.ItemSpec);
            
            var files = RoslynGenerator.GetFilePathsForGeneratedFiles(asm, OutputDirectory, GeneratedCodeFolder);

            GeneratedFiles = files
                .Select(file =>
                {
                    var item = new TaskItem(file);
                    return item;
                }).ToArray();
            return true;
        }
    }
}
