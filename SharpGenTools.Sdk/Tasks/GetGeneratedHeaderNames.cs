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
            var (headers, extensionHeaders) = config.GetFilesWithIncludesAndExtensionHeaders();
            Headers = headers.Select(CreateHeaderItem).ToArray<ITaskItem>();
            ExtensionHeaders = extensionHeaders.Select(CreateExtensionHeaderItem).ToArray<ITaskItem>();

            return true;
        }

        private TaskItem CreateExtensionHeaderItem(string cfg)
        {
            var item = new TaskItem(Path.Combine(OutputPath, $"{cfg}-ext.h"));
            item.SetMetadata("ConfigId", cfg);
            return item;
        }

        private TaskItem CreateHeaderItem(string cfg)
        {
            var item = new TaskItem(Path.Combine(OutputPath, $"{cfg}.h"));
            item.SetMetadata("ConfigId", cfg);
            return item;
        }
    }
}
