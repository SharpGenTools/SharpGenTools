using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Framework;
using Logger = SharpGen.Logging.Logger;
using Task = Microsoft.Build.Utilities.Task;

namespace SharpGenTools.Sdk.Tasks;

public abstract class SharpTaskBase : Task, ICancelableTask
{
    private volatile bool isCancellationRequested;

    // ReSharper disable MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
    [Required] public bool DebugWaitForDebuggerAttach { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global, MemberCanBePrivate.Global

    protected Logger SharpGenLogger { get; private set; }

    protected bool IsCancellationRequested => isCancellationRequested;

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
    private void WaitForDebuggerAttach()
    {
        if (!Debugger.IsAttached)
        {
            SharpGenLogger.Warning(null, $"{GetType().Name} is waiting for attach: {Process.GetCurrentProcess().Id}");
            Thread.Yield();
        }

        while (!Debugger.IsAttached && !IsCancellationRequested)
            Thread.Sleep(TimeSpan.FromSeconds(1));
    }

    public void Cancel()
    {
        isCancellationRequested = true;
    }
}