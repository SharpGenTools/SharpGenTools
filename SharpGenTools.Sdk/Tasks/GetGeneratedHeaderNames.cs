using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpGenTools.Sdk.Tasks
{
    public class GetGeneratedHeaderNames : SharpGenTaskBase
    {
        [Required]
        public string OutputPath { get; set; }

        [Output]
        public ITaskItem[] Headers { get; set; }

        [Output]
        public ITaskItem[] ExtensionHeaders { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            config.GetFilesWithIncludesAndExtensionHeaders(out var headers, out var extensionHeaders);
            Headers = headers.Select(CreateHeaderItem).ToArray<ITaskItem>();
            ExtensionHeaders = extensionHeaders.Select(CreateExtensionHeaderItem).ToArray<ITaskItem>();

            return true;
        }

        private TaskItem CreateExtensionHeaderItem(ConfigFile cfg)
        {
            var item = new TaskItem(Path.Combine(OutputPath, cfg.ExtensionFileName));
            item.SetMetadata("ConfigId", cfg.Id);
            return item;
        }

        private TaskItem CreateHeaderItem(ConfigFile cfg)
        {
            var item = new TaskItem(Path.Combine(OutputPath, cfg.HeaderFileName));
            item.SetMetadata("ConfigId", cfg.Id);
            return item;
        }
    }
}
