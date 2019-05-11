using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Model
{
    [Flags]
    public enum PlatformDetectionType
    {
        Any       = 0,
        IsWindows = 0b000001,
        IsSystemV = 0b000010
    }
}
