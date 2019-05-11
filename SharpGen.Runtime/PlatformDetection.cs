using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Runtime
{
    public static class PlatformDetection
    {
#if WIN
        public static bool IsWindows => true;
        public static bool IsSystemV => false;
#elif UNIX
        public static bool IsWindows => false;
        public static bool IsSystemV => true;
#else
        public static bool IsWindows => throw null;
        public static bool IsSystemV => throw null;
#endif
    }
}
