#nullable enable
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

/// <summary>
/// Vtbl attribute is used to associate a C++ callable managed interface to its virtual method table static type.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class VtblAttribute : Attribute
{
    /// <summary>
    /// Type of the associated virtual method table
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="VtblAttribute"/> class.
    /// </summary>
    /// <param name="holder">Type of the associated virtual method table</param>
    public VtblAttribute(Type holder)
    {
        Type = holder ?? throw new ArgumentNullException(nameof(holder));

        Debug.Assert(Type.GetTypeInfo() is { IsClass: true, IsAbstract: true, IsSealed: true });
    }

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static VtblAttribute? Get(Type type) => Get(type.GetTypeInfo());
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static VtblAttribute? Get(TypeInfo type) => type.GetCustomAttribute<VtblAttribute>();
    internal static bool Has(Type type) => Get(type.GetTypeInfo()) != null;
    internal static bool Has(TypeInfo type) => Get(type) != null;
}