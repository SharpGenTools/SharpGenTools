using System;
using System.Diagnostics.CodeAnalysis;

namespace SharpGen.Runtime.Trimming
{
    /// <summary>
    /// Helper class used for preserving metadata of given types.
    /// </summary>
    public static class TrimmingHelpers
    {
        // Note: Would rather use Generic <T> here, but `DynamicallyAccessedMembers.Interfaces` doesn't properly work
        // and doesn't preserve methods, only the list of interfaces that were inherited. Might be linker bug.

        /// <summary>
        /// Dummy method used for telling the IL Linker to preserve interfaces.
        /// </summary>
        /// <param name="type">Type to preserve interfaces for.</param>
        public static void PreserveInterfaces(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
            Type type) {}
    }
}
