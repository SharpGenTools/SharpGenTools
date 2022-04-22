// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

/// <summary>
/// A callback interface shadow, used to supplement interface implementers with additional logic,
/// possibly to modify marshalling behavior.
/// Using shadows incurs minor negative memory and performance impact, so use sparingly.
/// In terms of .NET runtime, it's a COM Callable Wrapper (CCW).
/// </summary>
public abstract unsafe class CppObjectShadow
{
    private GCHandle callbackHandle;

    static CppObjectShadow()
    {
        Debug.Assert(Marshal.SizeOf(typeof(CppObjectNative)) == CppObjectNative.Size);
        Debug.Assert(sizeof(CppObjectNative) == CppObjectNative.Size);
    }

    protected CppObjectShadow()
    {
        Debug.Assert(this is not ICallbackable);
    }

    internal void Initialize(GCHandle callbackHandle)
    {
        Debug.Assert(!this.callbackHandle.IsAllocated);
        Debug.Assert(callbackHandle.IsAllocated);
        Debug.Assert(callbackHandle.Target is CallbackBase);
        Debug.Assert(callbackHandle.Target is ICallbackable);
        this.callbackHandle = callbackHandle;
    }

    internal static IntPtr CreateCallableWrapper(GCHandle callback, void* vtbl)
    {
        // Allocate ptr to vtbl + ptr to callback together
        var nativePointer = Marshal.AllocHGlobal(CppObjectNative.Size);
        ref var native = ref *(CppObjectNative*) nativePointer;

        Debug.Assert(callback.IsAllocated);

        native.VtblPointer = vtbl;
        native.Shadow = callback;

        return nativePointer;
    }

    internal static void FreeCallableWrapper(IntPtr pointer, bool disposing)
    {
        // Free the callback
        if (((CppObjectNative*) pointer)->Shadow is { IsAllocated: true, Target: CppObjectShadow shadow } handle)
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

    public static T ToShadow<T>(IntPtr thisPtr) where T : CppObjectShadow
    {
        Debug.Assert(thisPtr != IntPtr.Zero);

        var handle = ((CppObjectNative*) thisPtr)->Shadow;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T shadow => shadow,
            null => throw new Exception($"Shadow {typeof(T).FullName} is dead"),
            { } value => throw new Exception(
                             $"Shadow is of an unexpected type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }

    public static T ToCallback<T>(IntPtr thisPtr) where T : ICallbackable
    {
        Debug.Assert(thisPtr != IntPtr.Zero);

        var handle = ((CppObjectNative*) thisPtr)->Shadow;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T value => value,
            CppObjectShadow shadow => shadow.ToCallback<T>(),
            null => throw new Exception($"Shadow {typeof(T).FullName} is dead"),
            { } value => throw new Exception(
                             $"Shadow is of an unexpected type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }

    public T ToCallback<T>() where T : ICallbackable
    {
        var handle = callbackHandle;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T value => value,
            null => throw new Exception($"Shadow {typeof(T).FullName} references dead callback"),
            { } value => throw new Exception(
                             $"Shadow references an unexpected callback of type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }

    private ref struct CppObjectNative
    {
        internal static readonly int Size = IntPtr.Size * 2;

        // ReSharper disable once NotAccessedField.Local
        public void* VtblPointer;
        private IntPtr _shadowPointer;

        public GCHandle Shadow
        {
            readonly get => GCHandle.FromIntPtr(_shadowPointer);
            set => _shadowPointer = GCHandle.ToIntPtr(value);
        }
    }
}