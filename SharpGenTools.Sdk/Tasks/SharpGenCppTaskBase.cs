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
            Log.LogMessage("Resolving SDKs...");
            foreach (var cfg in config.ConfigFilesLoaded)
            {
                Log.LogMessage($"Resolving SDK for Config {cfg}");
                foreach (var sdk in cfg.Sdks)
                {
                    Log.LogMessage($"Resolving {sdk.Name}: Version {sdk.Version}");
                    foreach (var directory in sdkResolver.ResolveIncludeDirsForSdk(sdk))
                    {
                        Log.LogMessage($"Resolved include directory {directory}");
                        cfg.IncludeDirs.Add(directory); 
                    }
                }
            }
            return config;
        }
    }
}
