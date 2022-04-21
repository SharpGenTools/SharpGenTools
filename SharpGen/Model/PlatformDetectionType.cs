using System;

namespace SharpGen.Model;

[Flags]
public enum PlatformDetectionType
{
    Windows = 1 << 0,
    ItaniumSystemV = 1 << 1,
    Any = Windows | ItaniumSystemV
}