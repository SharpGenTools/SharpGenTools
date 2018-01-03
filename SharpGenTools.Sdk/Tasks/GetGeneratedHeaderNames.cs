using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGenTools.Sdk.Tasks
{
    public class GetGeneratedHeaderNames : SharpGenTaskBase
    {
        [Output]
        public ITaskItem[] Headers { get; set; }

        [Output]
        public ITaskItem[] ExtensionHeaders { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var (headers, extensionHeaders) = config.GetFilesWithIncludesAndExtensionHeaders();
            Headers = headers.Select(cfg => CreateHeaderItem(cfg)).ToArray();
            ExtensionHeaders = extensionHeaders.Select(cfg => CreateExtensionHeaderItem(cfg)).ToArray();

            return true;
        }

        private static TaskItem CreateExtensionHeaderItem(string cfg)
        {
            var item = new TaskItem($"{cfg}-ext.h");
            item.SetMetadata("ConfigId", cfg);
            return item;
        }

        private static TaskItem CreateHeaderItem(string cfg)
        {
            var item = new TaskItem($"{cfg}.h");
            item.SetMetadata("ConfigId", cfg);
            return item;
        }
    }
}
