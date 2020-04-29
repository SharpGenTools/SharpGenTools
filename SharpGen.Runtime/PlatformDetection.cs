using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static class PlatformDetection
    {
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static readonly bool IsItaniumSystemV = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                                       RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
