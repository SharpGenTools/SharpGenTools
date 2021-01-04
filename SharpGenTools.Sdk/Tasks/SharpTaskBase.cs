using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Utilities;
using Logger = SharpGen.Logging.Logger;

namespace SharpGenTools.Sdk.Tasks
{
    public abstract class SharpTaskBase : Task
    {
        // ReSharper disable MemberCanBeProtected.Global, UnusedAutoPropertyAccessor.Global
        public bool DebugWaitForDebuggerAttach { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global, MemberCanBeProtected.Global

        protected Logger SharpGenLogger { get; private set; }

        protected void PrepareExecute()
        {
            BindingRedirectResolution.Enable();

            SharpGenLogger = new Logger(new MSBuildSharpGenLogger(Log));

#if DEBUG
            if (DebugWaitForDebuggerAttach)
                WaitForDebuggerAttach();
#endif
        }

        [Conditional("DEBUG")]
        protected void WaitForDebuggerAttach()
        {
            if (!Debugger.IsAttached)
            {
                SharpGenLogger.Warning(null, $"{GetType().Name} is waiting for attach: {Process.GetCurrentProcess().Id}");
                Thread.Yield();
            }

            while (!Debugger.IsAttached)
                Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}