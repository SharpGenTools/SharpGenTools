using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static class PlatformDetection
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsItaniumSystemV => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
