using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        ref var guid = ref Storage<T>.Guid;

        if (guid != default)
            return guid;

#if NETSTANDARD1_3
        return guid = typeof(T).GetTypeInfo().GUID;
#else
        return guid = typeof(T).GUID;
#endif
    }

    internal static void Register<T>(void* vtbl) where T : ICallbackable
    {
        Debug.Assert(Storage<T>.TargetVtbl is null);
        Storage<T>.TargetVtbl = vtbl;
        vtblByGuid[GetGuid<T>()] = new IntPtr(vtbl);
    }

    internal static bool GetVtbl(Guid guid, out void* pointer)
    {
        var result = vtblByGuid.TryGetValue(guid, out var ptr);
        pointer = ptr.ToPointer();
        return result;
    }

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static class Storage<T> where T : ICallbackable
    {
        public static Guid Guid;
        public static IntPtr[] SourceVtbl;
        public static void* TargetVtbl;
    }
}