using System;
using System.Diagnostics.CodeAnalysis;

namespace SharpGen.Runtime.Trimming
{
    /// <summary>
    /// Helper class used for preserving metadata of given types.
    /// </summary>
    public static class TrimmingHelpers
    {
        /// <summary>
        /// Dummy method used for telling the IL Linker to preserve the type in its entirety.
        /// </summary>
        /// <typeparam name="T">Type to preserve all implementation for.</typeparam>
        public static void PreserveMe<
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
# endif
            T>() {}
    }
}
