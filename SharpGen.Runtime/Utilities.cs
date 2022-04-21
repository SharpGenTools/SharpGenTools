using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

internal static class Utilities
{
#if NETCOREAPP3_0_OR_GREATER
        internal const MethodImplOptions MethodAggressiveOptimization = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
    internal const MethodImplOptions MethodAggressiveOptimization = MethodImplOptions.AggressiveInlining;
#endif

    [MethodImpl(MethodAggressiveOptimization)]
    internal static string FormatPointer(IntPtr nativePointer) =>
        nativePointer == IntPtr.Zero ? "NULL" : "0x" + nativePointer.ToInt64().ToString("X");
}