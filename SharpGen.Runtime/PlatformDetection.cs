// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static partial class PlatformDetection
    {
        private const string NtDll = "ntdll.dll";
        private const string Advapi32 = "advapi32.dll";
        private const string Kernel32 = "kernel32.dll";

        private static volatile bool _isAppContainerProcess;
        private static volatile bool _isAppContainerProcessInitialized;

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static readonly bool IsItaniumSystemV = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                                       RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsAppContainerProcess
        {
            get
            {
                if (!_isAppContainerProcessInitialized)
                {
                    if (IsWindows)
                    {
                        var osVersion = OSVersion;

                        if (osVersion.Major < 6 || osVersion.Major == 6 && osVersion.Minor <= 1)
                            // Windows 7 or older.
                            _isAppContainerProcess = false;
                        else
                            _isAppContainerProcess = HasAppContainerToken();
                    }
                    else
                    {
                        _isAppContainerProcess = false;
                    }

                    _isAppContainerProcessInitialized = true;
                }

                return _isAppContainerProcess;
            }
        }
    }
}