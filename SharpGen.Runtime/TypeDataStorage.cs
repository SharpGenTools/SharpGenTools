// #define FORCE_REFLECTION_ONLY

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SharpGen.Runtime.Trimming;

namespace SharpGen.Runtime;

public static unsafe class TypeDataStorage
{
    private static readonly ConcurrentDictionary<Guid, IntPtr> vtblByGuid = new();

    static TypeDataStorage()
    {
        Storage<IUnknown>.Guid = new Guid(0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        Storage<IInspectable>.Guid = new Guid(0xAF86E2E0, 0xB12D, 0x4C6A, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90);
        Storage<IUnknown>.SourceVtbl = ComObjectVtbl.Vtbl;
        Storage<IInspectable>.SourceVtbl = InspectableVtbl.Vtbl;
        TypeDataRegistrationHelper helper = new();
        {
            helper.Add(ComObjectVtbl.Vtbl);
            helper.Register<IUnknown>();
        }
        {
            helper.Add(ComObjectVtbl.Vtbl);
            helper.Add(InspectableVtbl.Vtbl);
            helper.Register<IInspectable>();
        }
    }

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    internal static Guid GetGuid<T>() where T : ICallbackable
    {
#if !FORCE_REFLECTION_ONLY
        ref var guid = ref Storage<T>.Guid;

        if (guid != default)
            return guid;

#if NETSTANDARD1_3
        return guid = typeof(T).GetTypeInfo().GUID;
#else
        return guid = typeof(T).GUID;
#endif
#else
#if NETSTANDARD1_3
        return typeof(T).GetTypeInfo().GUID;
#else
        return typeof(T).GUID;
#endif
#endif
    }

    internal static void Register<T>(void* vtbl) where T : ICallbackable
    {
#if !FORCE_REFLECTION_ONLY
        Register(GetGuid<T>(), vtbl);
#endif
    }

    internal static void Register(Guid guid, void* vtbl) => vtblByGuid[guid] = new IntPtr(vtbl);

    internal static IntPtr[]? GetSourceVtbl<T>() where T : ICallbackable
    {
#if !FORCE_REFLECTION_ONLY
        if (Storage<T>.SourceVtbl is { } storedVtbl)
            return storedVtbl;
#endif

        return GetSourceVtblFromReflection(typeof(T));
    }

    private static IntPtr[]? GetSourceVtblFromReflection(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type)
    {
        const string vtbl = "Vtbl";

        var vtblAttribute = VtblAttribute.Get(type);
        Debug.Assert(vtblAttribute is not null, $"Type {type.FullName} has no Vtbl attribute");

        var vtblType = vtblAttribute!.Type;

#if NETSTANDARD1_3
        static bool Predicate(MemberInfo x) => x.Name == vtbl;

        if (vtblType.GetRuntimeFields().FirstOrDefault(Predicate)?.GetValue(null) is IntPtr[] vtblFieldValue)
        {
            return vtblFieldValue;
        }

        if (vtblType.GetRuntimeProperties().FirstOrDefault(Predicate)?.GetValue(null) is IntPtr[] vtblPropertyValue)
        {
            return vtblPropertyValue;
        }
#else
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static;

        if (vtblType.GetField(vtbl, flags)?.GetValue(null) is IntPtr[] vtblFieldValue)
        {
            return vtblFieldValue;
        }

        if (vtblType.GetProperty(vtbl, flags)?.GetValue(null) is IntPtr[] vtblPropertyValue)
        {
            return vtblPropertyValue;
        }
#endif

        Debug.Fail($"Type {type.FullName} has no Vtbl field or property");

        return null;
    }

    internal static bool GetTargetVtbl(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        TypeInfo type, out void* pointer)
    {
#if !FORCE_REFLECTION_ONLY
        if (vtblByGuid.TryGetValue(type.GUID, out var ptr))
        {
            pointer = ptr.ToPointer();
            return true;
        }
#endif

        if (GetSourceVtblFromReflection(type.AsType()) is { } sourceVtbl)
        {
            pointer = RegisterFromReflection(type, sourceVtbl).ToPointer();
            return true;
        }

        pointer = default;
        return false;
    }

    private static IntPtr RegisterFromReflection(
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        TypeInfo type, IntPtr[] sourceVtbl)
    {
        var callbackable = typeof(ICallbackable).GetTypeInfo();

        TypeDataRegistrationHelper helper = new();
        List<RegisterInheritanceItem> items = new();

        foreach (var iface in type.ImplementedInterfaces)
        {
            var typeInfo = iface.GetTypeInfoWithNestedPreservedInterfaces();
            if (callbackable == typeInfo || !callbackable.IsAssignableFrom(typeInfo))
                continue;

            var iSourceVtbl = GetSourceVtblFromReflection(iface);
            if (iSourceVtbl is null)
                throw new Exception($"Failed to reflect Vtbl out of an {nameof(ICallbackable)} interface '{iface.FullName}'.");

            items.Add(new RegisterInheritanceItem(typeInfo, typeInfo.ImplementedInterfaces.Count(), iSourceVtbl));
        }

        // TODO: properly sort items by inheritance
        // TODO: verify single inheritance
        items.Sort(static (x, y) => Comparer<int>.Default.Compare(x.InterfaceCount, y.InterfaceCount));

        foreach (var (_, _, iSourceVtbl) in items)
            helper.Add(iSourceVtbl);

        helper.Add(sourceVtbl);
        return helper.Register(type);
    }

    private record struct RegisterInheritanceItem(TypeInfo Type, int InterfaceCount, IntPtr[] SourceVtbl);

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static class Storage<T> where T : ICallbackable
    {
        public static Guid Guid;
        public static IntPtr[]? SourceVtbl;
    }
}