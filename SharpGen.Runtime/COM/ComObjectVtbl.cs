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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

public static unsafe class ComObjectVtbl
{
#if NET6_0_OR_GREATER
    public static readonly IntPtr[] Vtbl =
    {
        (IntPtr) (delegate* unmanaged<IntPtr, Guid*, void*, int>) (&QueryInterfaceImpl),
        (IntPtr) (delegate* unmanaged<IntPtr, uint>) (&AddRefImpl),
        (IntPtr) (delegate* unmanaged<IntPtr, uint>) (&ReleaseImpl)
    };
#else
    private static readonly QueryInterfaceDelegate QueryInterfaceDelegateCache = QueryInterfaceImpl;
    private static readonly AddRefDelegate AddRefDelegateCache = AddRefImpl;
    private static readonly ReleaseDelegate ReleaseDelegateCache = ReleaseImpl;
    public static readonly IntPtr[] Vtbl =
    {
        Marshal.GetFunctionPointerForDelegate(QueryInterfaceDelegateCache),
        Marshal.GetFunctionPointerForDelegate(AddRefDelegateCache),
        Marshal.GetFunctionPointerForDelegate(ReleaseDelegateCache)
    };
#endif

#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int QueryInterfaceDelegate(IntPtr thisObject, Guid* guid, void* output);
#else
    [UnmanagedCallersOnly]
#endif
    private static int QueryInterfaceImpl(IntPtr thisObject, Guid* guid, void* output)
    {
        var callback = CppObjectShadow.ToCallback<CallbackBase>(thisObject);
#if DEBUG
        try
        {
#endif
            ref var ppvObject = ref Unsafe.AsRef<IntPtr>(output);
            var result = callback.Find(*guid);
            ppvObject = result;

            if (result == IntPtr.Zero)
                return Result.NoInterface.Code;

            MarshallingHelpers.AddRef(callback);

            return Result.Ok.Code;
#if DEBUG
        }
        catch (Exception exception)
        {
            (callback as IExceptionCallback)?.RaiseException(exception);
            return Result.GetResultFromException(exception).Code;
        }
#endif
    }

#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint AddRefDelegate(IntPtr thisObject);
#else
    [UnmanagedCallersOnly]
#endif
    private static uint AddRefImpl(IntPtr thisObject) =>
        MarshallingHelpers.AddRef(CppObjectShadow.ToCallback<IUnknown>(thisObject));

#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseDelegate(IntPtr thisObject);
#else
    [UnmanagedCallersOnly]
#endif
    private static uint ReleaseImpl(IntPtr thisObject) =>
        MarshallingHelpers.Release(CppObjectShadow.ToCallback<IUnknown>(thisObject));
}