#nullable enable

using System;
#if NETSTANDARD1_1
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace SharpGen.Runtime
{
    public static partial class PlatformDetection
    {
#if NETSTANDARD1_1
        private static Version? _osVersion;

        public static Version OSVersion
        {
            get
            {
                if (_osVersion == null)
                {
                    Interlocked.CompareExchange(ref _osVersion, GetOSVersion(), null);
                }

                return _osVersion;
            }
        }

        [DllImport(NtDll, ExactSpelling = true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        private static unsafe int RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi)
        {
            osvi = default;
            osvi.dwOSVersionInfoSize = (uint) sizeof(RTL_OSVERSIONINFOEX);
            return RtlGetVersion(ref osvi);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct RTL_OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            public fixed char szCSDVersion[128];
        }

        private static Version GetOSVersion()
        {
            if (RtlGetVersionEx(out var osvi) != 0)
            {
                throw new InvalidOperationException("RtlGetVersion call failed.");
            }

            return new Version((int) osvi.dwMajorVersion, (int) osvi.dwMinorVersion, (int) osvi.dwBuildNumber, 0);
        }
#else
        public static Version OSVersion => Environment.OSVersion.Version;
#endif
    }
}