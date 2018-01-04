using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.Config;
using SharpGen.Parser;

namespace SharpGenTools.Sdk.Tasks
{
    public abstract class SharpGenCppTaskBase : SharpGenTaskBase
    {
        protected override ConfigFile LoadConfig(ConfigFile config)
        {
            config = base.LoadConfig(config);
            var sdkResolver = new SdkResolver(SharpGenLogger);
            foreach (var cfg in config.ConfigFilesLoaded)
            {
                foreach (var sdk in config.Sdks)
                {
                    cfg.IncludeDirs.AddRange(sdkResolver.ResolveIncludeDirsForSdk(sdk));
                }
            }
            return config;
        }
    }
}
