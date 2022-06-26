using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SharpGen.Runtime.TrimmingWrappers
{
    internal static class TrimmingExtensions
    {
#if NET6_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2068", Justification = "We're preserving nested interfaces via wrapper method.")]
#endif
        public static TypeInfo GetTypeInfoWithPreservedInterfaces(this Type type)
        {
            return type.GetTypeInfo();
        }


#if NET6_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "We're preserving nested interfaces via wrapper method.")]
#endif
        public static Type GetTypeWithPreservedInterfaces(this object obj)
        {
            return obj.GetType();
        }
    }
}
