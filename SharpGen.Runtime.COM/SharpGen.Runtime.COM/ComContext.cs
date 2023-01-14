using System;

namespace SharpGen.Runtime
{
    /// <summary>
    ///     Values that are used in activation calls to indicate the execution contexts in which an object is to be run.
    /// </summary>
    [Flags]
    public enum ComContext : uint
    {
        /// <summary>
        ///     The code that creates and manages objects of this class is a DLL that runs in the same process as the caller of the
        ///     function specifying the class context.
        /// </summary>
        InprocServer = 0x1,

        /// <summary>
        ///     The code that manages objects of this class is an in-process handler. This is a DLL that runs in the client process
        ///     and implements client-side structures of this class when instances of the class are accessed remotely.
        /// </summary>
        InprocHandler = 0x2,

        /// <summary>
        ///     The EXE code that creates and manages objects of this class runs on same machine but is loaded in a separate
        ///     process space.
        /// </summary>
        LocalServer = 0x4,

        /// <summary>
        ///     16-bit server dll (runs in same process as caller)
        /// </summary>
        InprocServer16 = 0x8,

        /// <summary>
        ///     A remote context. The LocalServer32 or LocalService code that creates and manages objects of this class is run on a
        ///     different computer.
        /// </summary>
        RemoteServer = 0x10,

        /// <summary>
        ///     16-bit handler dll (runs in same process as caller)
        /// </summary>
        InprocHandler16 = 0x20,

        /// <summary>
        ///     Formerly INPROC_SERVERX86, deprecated
        /// </summary>
        Reserved1 = 0x40,

        /// <summary>
        ///     Formerly INPROC_HANDLERX86, deprecated
        /// </summary>
        Reserved2 = 0x80,

        /// <summary>
        ///     Formerly ESERVER_HANDLER, deprecated
        /// </summary>
        Reserved3 = 0x100,

        /// <summary>
        ///     Formerly CLSCTX_KERNEL_SERVER, now used only in kmode
        /// </summary>
        Reserved4 = 0x200,

        /// <summary>
        ///     Disallow code download from the Directory Service (if any) or the internet
        /// </summary>
        NoCodeDownload = 0x400,

        /// <summary>
        ///     Formerly NO_WX86_TRANSLATION, deprecated
        /// </summary>
        Reserved5 = 0x800,

        /// <summary>
        ///     Specify if you want the activation to fail if it uses custom marshalling.
        /// </summary>
        NoCustomMarshal = 0x1000,

        /// <summary>
        ///     Allow code download from the Directory Service (if any) or the internet
        /// </summary>
        EnableCodeDownload = 0x2000,

        /// <summary>
        ///     Do not log messages about activation failure (should one occur) to Event Log
        /// </summary>
        NoFailureLog = 0x4000,

        /// <summary>
        ///     Disable activate-as-activator capability for this activation only
        /// </summary>
        DisableAaa = 0x8000,

        /// <summary>
        ///     Enable activate-as-activator capability for this activation only
        /// </summary>
        EnableAaa = 0x10000,

        /// <summary>
        ///     Begin this activation from the default context of the current apartment
        /// </summary>
        FromDefaultContext = 0x20000,

        /// <summary>
        ///     Pick x86 server only
        /// </summary>
        ActivateX86Server = 0x40000,

        /// <summary>
        ///     Old name for CLSCTX_ACTIVATE_X86_SERVER; value must be identical for compatibility
        /// </summary>
        Activate32BitServer = ActivateX86Server,

        /// <summary>
        ///     Pick 64-bit server only
        /// </summary>
        Activate64BitServer = 0x80000,

        /// <summary>
        ///     Use the impersonation thread token (if present) for the activation.
        /// </summary>
        EnableCloaking = 0x100000,

        /// <summary>
        ///     Internal CLSCTX flag used to indicate activation is for app container
        /// </summary>
        AppContainer = 0x400000,

        /// <summary>
        ///     Interactive User activation behavior for As-Activator servers.
        /// </summary>
        ActivateAaaAsIu = 0x800000,

        /// <summary>
        ///     reserved
        /// </summary>
        Reserved6 = 0x1000000,

        /// <summary>
        ///     Pick ARM32 server only
        /// </summary>
        ActivateArm32Server = 0x2000000,

        /// <summary>
        ///     Internal CLSCTX flag used for loading Proxy/Stub DLLs
        /// </summary>
        PsDll = 0x80000000,

        Inproc = InprocServer | InprocHandler,
        Server = InprocServer | LocalServer | RemoteServer,
        All = Server | InprocHandler
    }
}