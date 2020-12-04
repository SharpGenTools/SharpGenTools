using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Config;
using Logger = SharpGen.Logging.Logger;

namespace SharpGenTools.Sdk.Tasks
{
    public abstract class SharpGenTaskBase : Task
    {
        [Required] public ITaskItem[] ConfigFiles { get; set; }

        public string[] Macros { get; set; }

#if DEBUG
        public bool DebugWaitForDebuggerAttach { get; set; }
#endif

        protected Logger SharpGenLogger { get; set; }

        public sealed override bool Execute()
        {
            PrepareExecute();

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

        protected void PrepareExecute()
        {
            BindingRedirectResolution.Enable();

#if DEBUG
            if (DebugWaitForDebuggerAttach)
                WaitForDebuggerAttach();
#endif

            SharpGenLogger = new Logger(new MSBuildSharpGenLogger(Log));
        }

        [Conditional("DEBUG")]
        protected internal static void WaitForDebuggerAttach()
        {
            while (!Debugger.IsAttached)
                Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}