using System;

namespace SharpGen.Model
{
    [Flags]
    public enum PlatformDetectionType
    {
        IsWindows = 0b000001,
        IsItaniumSystemV = 0b000010,
        Any = IsWindows | IsItaniumSystemV
    }
}
