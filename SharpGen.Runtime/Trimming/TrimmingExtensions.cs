using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SharpGen.Runtime.Trimming
{
    internal static class TrimmingExtensions
    {
        /*
            These extensions are implemented in this way to show intent based on usage [suppressing un-analyzable patterns].

            Given the analyzer code, seen below 
            https://github.com/dotnet/linker/blob/a66609adf79440272e6522c29e8a90291030125b/src/ILLink.RoslynAnalyzer/DataFlow/DynamicallyAccessedMembersBinder.cs#L341

            Nested interfaces should already be preserved when `DynamicallyAccessedMemberTypes.Interfaces` is specified.  
        */

#if NET6_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2068", Justification = "Nested interfaces are already preserved.")]
#endif
        public static TypeInfo GetTypeInfoWithNestedPreservedInterfaces(this Type type)
        {
            return type.GetTypeInfo();
        }

#if NET6_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "Nested interfaces are already preserved.")]
#endif
        public static Type GetTypeWithNestedPreservedInterfaces(this object obj)
        {
            return obj.GetType();
        }
    }
}
