using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class ExcludeFromTypeListAttribute : Attribute
{
    /// <summary>
    /// Check presence of <see cref="ExcludeFromTypeListAttribute"/> on the specified type.
    /// </summary>
    /// <returns>true if attribute was found on the specified type</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static bool Has(Type type) => Has(type.GetTypeInfo());

    /// <summary>
    /// Check presence of <see cref="ExcludeFromTypeListAttribute"/> on the specified type.
    /// </summary>
    /// <returns>true if attribute was found on the specified type</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static bool Has(TypeInfo type) => type.GetCustomAttribute<ExcludeFromTypeListAttribute>() != null;
}