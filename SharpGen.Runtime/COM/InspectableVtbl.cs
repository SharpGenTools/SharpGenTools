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
using SharpGen.Runtime.Win32;

namespace SharpGen.Runtime;

public static unsafe class InspectableVtbl
{
#if NET6_0_OR_GREATER
    public static readonly IntPtr[] Vtbl =
    {
        (IntPtr) (delegate *unmanaged<IntPtr, int*, IntPtr**, int>) (&GetIids),
        (IntPtr) (delegate *unmanaged<IntPtr, IntPtr*, int>) (&GetRuntimeClassName),
        (IntPtr) (delegate *unmanaged<IntPtr, int*, int>) (&GetTrustLevel)
    };
#else
    private static readonly GetIidsDelegate GetIidsDelegateCache = GetIids;
    private static readonly GetRuntimeClassNameDelegate GetRuntimeClassNameDelegateCache = GetRuntimeClassName;
    private static readonly GetTrustLevelDelegate GetTrustLevelDelegateCache = GetTrustLevel;
    public static readonly IntPtr[] Vtbl =
    {
        Marshal.GetFunctionPointerForDelegate(GetIidsDelegateCache),
        Marshal.GetFunctionPointerForDelegate(GetRuntimeClassNameDelegateCache),
        Marshal.GetFunctionPointerForDelegate(GetTrustLevelDelegateCache)
    };
#endif

    /// <unmanaged>
    /// HRESULT STDMETHODCALLTYPE GetIids(
    ///   /* [out] */ __RPC__out ULONG *iidCount,
    ///   /* [size_is][size_is][out] */ __RPC__deref_out_ecount_full_opt(*iidCount) IID **iids
    /// )
    /// </unmanaged>
#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetIidsDelegate(IntPtr thisPtr, int* iidCount, IntPtr** iids);
#else
    [UnmanagedCallersOnly]
#endif
    private static int GetIids(IntPtr thisPtr, int* iidCount, IntPtr** iids)
    {
        var @this = CppObjectShadow.ToCallback<CallbackBase>(thisPtr);
        try
        {
            var guids = @this.Guids;
            var countGuids = guids.Length;
            var iidsMemory = Marshal.AllocCoTaskMem(IntPtr.Size * countGuids);

            // Copy GUIDs deduced from Callback
            *iids = (IntPtr*) iidsMemory;
            *iidCount = countGuids;

            MemoryHelpers.CopyMemory(iidsMemory, guids);

            return Result.Ok.Code;
        }
        catch (Exception exception)
        {
            (@this as IExceptionCallback)?.RaiseException(exception);
            return Result.GetResultFromException(exception).Code;
        }
    }

    /// <unmanaged>
    /// HRESULT STDMETHODCALLTYPE GetRuntimeClassName([out] __RPC__deref_out_opt HSTRING *className)
    /// </unmanaged>
#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetRuntimeClassNameDelegate(IntPtr thisPtr, IntPtr* className);
#else
    [UnmanagedCallersOnly]
#endif
    private static int GetRuntimeClassName(IntPtr thisPtr, IntPtr* className)
    {
        var @this = CppObjectShadow.ToCallback<IInspectable>(thisPtr);
        try
        {
            var result = new WinRTString(
                @this switch
                {
                    IInspectableWithRuntimeClassName { RuntimeClassName: var runtimeClassName } => runtimeClassName,
                    _ => @this.GetType().FullName
                }
            );

            *className = result.NativePointer;

            return Result.Ok.Code;
        }
        catch (Exception exception)
        {
            (@this as IExceptionCallback)?.RaiseException(exception);
            return Result.GetResultFromException(exception).Code;
        }
    }

    /// <unmanaged>
    /// HRESULT STDMETHODCALLTYPE GetTrustLevel(/* [out] */ __RPC__out TrustLevel *trustLevel);
    /// </unmanaged>
#if !NET6_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetTrustLevelDelegate(IntPtr thisPtr, int* trustLevel);
#else
    [UnmanagedCallersOnly]
#endif
    private static int GetTrustLevel(IntPtr thisPtr, int* trustLevel)
    {
        var @this = CppObjectShadow.ToCallback<IInspectable>(thisPtr);
        try
        {
            *trustLevel = @this switch
            {
                IInspectableWithTrustLevel { TrustLevel: var level } => (int) level,
                _ => (int) TrustLevel.BaseTrust
            };

            return Result.Ok.Code;
        }
        catch (Exception exception)
        {
            (@this as IExceptionCallback)?.RaiseException(exception);
            return Result.GetResultFromException(exception).Code;
        }
    }
}