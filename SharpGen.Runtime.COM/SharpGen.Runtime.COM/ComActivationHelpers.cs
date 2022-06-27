using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static class ComActivationHelpers
    {
        private const uint RPC_E_CHANGED_MODE = 0x80010106;
        private const uint COINIT_MULTITHREADED = 0x0;
        private const uint COINIT_APARTMENTTHREADED = 0x2;

        public static void CoInitialize()
        {
            if (CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED) == RPC_E_CHANGED_MODE)
                CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
        }

        public static Result CreateComInstance(Guid classGuid, ComContext context, Guid interfaceGuid,
                                               out IntPtr comObject)
        {
            return PlatformDetection.IsAppContainerProcess
                       ? CreateComInstanceRestricted(classGuid, context, interfaceGuid, out comObject)
                       : CreateComInstanceUnrestricted(classGuid, context, interfaceGuid, out comObject);
        }

        #region Win32

        public static Result CreateComInstanceUnrestricted(Guid classGuid, ComContext context, Guid interfaceGuid,
                                                           out IntPtr comObject) =>
            CoCreateInstance(classGuid, IntPtr.Zero, context, interfaceGuid, out comObject);

        [DllImport("ole32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CoInitializeEx([In] [Optional] IntPtr pvReserved, [In] uint dwCoInit);

        [DllImport("ole32.dll", ExactSpelling = true,
                   EntryPoint = "CoCreateInstance", PreserveSig = true)]
        private static extern Result CoCreateInstance([In] [MarshalAs(UnmanagedType.LPStruct)]
                                                      Guid rclsid,
                                                      IntPtr pUnkOuter,
                                                      ComContext dwClsContext,
                                                      [In] [MarshalAs(UnmanagedType.LPStruct)]
                                                      Guid riid,
                                                      out IntPtr comObject);

        #endregion

        #region UWP

        public static unsafe Result CreateComInstanceRestricted(Guid classGuid, ComContext context, Guid interfaceGuid,
                                                                out IntPtr comObject)
        {
            var localQuery = new MultiQueryInterface
            {
                InterfaceIID = new IntPtr(&interfaceGuid),
                IUnknownPointer = IntPtr.Zero,
                ResultCode = 0
            };

            var result = CoCreateInstanceFromApp(classGuid, IntPtr.Zero, context, IntPtr.Zero, 1, ref localQuery);
            comObject = localQuery.IUnknownPointer;

            if (!result.Success)
                return result;

            if (!localQuery.ResultCode.Success)
                return localQuery.ResultCode;

            if (result != Result.Ok)
                return result;

            if (localQuery.ResultCode != Result.Ok)
                return localQuery.ResultCode;

            return Result.Ok;
        }

        [DllImport("api-ms-win-core-com-l1-1-0.dll", ExactSpelling = true,
                   EntryPoint = "CoCreateInstanceFromApp", PreserveSig = true)]
        private static extern Result CoCreateInstanceFromApp([In] [MarshalAs(UnmanagedType.LPStruct)]
                                                             Guid rclsid,
                                                             IntPtr pUnkOuter,
                                                             ComContext dwClsContext,
                                                             IntPtr reserved,
                                                             int countMultiQuery,
                                                             ref MultiQueryInterface query);

        [StructLayout(LayoutKind.Sequential)]
        private struct MultiQueryInterface
        {
            public IntPtr InterfaceIID;
            public IntPtr IUnknownPointer;
            public Result ResultCode;
        }

        #endregion
    }
}