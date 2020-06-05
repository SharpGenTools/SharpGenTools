using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logger = SharpGen.Logging.Logger;

namespace SharpGenTools.Sdk.Tasks
{
    public abstract class SharpGenTaskBase : Task
    {
        [Required]
        public ITaskItem[] ConfigFiles { get; set; }

        public string[] Macros { get; set; }

        protected Logger SharpGenLogger { get; private set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();

            SharpGenLogger = new Logger(new MSBuildSharpGenLogger(Log), null);

            var config = new ConfigFile
            {
                Files = ConfigFiles.Select(file => file.ItemSpec).ToList(),
                Id = "SharpGen-MSBuild"
            };

            try
            {
                config = LoadConfig(config);

                if (SharpGenLogger.HasErrors)
                {
                    return false;
                }

                return Execute(config);
            }
            catch (CodeGenFailedException ex)
            {
                Log.LogError(ex.Message);
                return false;
            }
        }

        protected virtual ConfigFile LoadConfig(ConfigFile config)
        {
            config = ConfigFile.Load(config, Macros, SharpGenLogger);
            return config;
        }

        protected abstract bool Execute(ConfigFile config);
    }
}
