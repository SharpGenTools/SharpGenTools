#nullable enable

using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

internal unsafe ref struct CppObjectCallableWrapper
{
    internal static readonly int Size = IntPtr.Size * 2;

    // ReSharper disable once NotAccessedField.Local
    private void* _vtbl;
    private IntPtr _shadow;

    public readonly GCHandle Shadow => GCHandle.FromIntPtr(_shadow);

    public static IntPtr Create(void* vtbl, GCHandle callback)
    {
        // Allocate ptr to vtbl + ptr to callback together
        var nativePointer = Marshal.AllocHGlobal(Size);
        ref var native = ref *(CppObjectCallableWrapper*) nativePointer;

        native._vtbl = vtbl;
        native._shadow = GCHandle.ToIntPtr(callback);

        return nativePointer;
    }

    public static void Free(IntPtr pointer, bool disposing)
    {
        // Free the callback
        if (((CppObjectCallableWrapper*) pointer)->Shadow is { IsAllocated: true, Target: CppObjectShadow shadow } handle)
        {
            // Callback is a CppObjectShadow subtype. Dispose it if needed.
            MemoryHelpers.Dispose(shadow, disposing);

            // Free GCHandle if it points to a shadow, not the CallbackBase. Why?
            // Same GCHandle is reused in multiple CCWs to lower the handle table pressure.
            handle.Free();
        }

        // Free instance
        Marshal.FreeHGlobal(pointer);
    }
}