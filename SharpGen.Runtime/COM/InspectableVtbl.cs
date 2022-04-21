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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpGen.Runtime.Win32;

namespace SharpGen.Runtime;

[DebuggerTypeProxy(typeof(CppObjectVtblDebugView))]
public unsafe class InspectableVtbl : ComObjectVtbl
{
    public InspectableVtbl(int numberOfCallbackMethods) : base(numberOfCallbackMethods + 3)
    {
#if NET5_0_OR_GREATER
            AddMethod((delegate *unmanaged[Stdcall]<IntPtr, int*, IntPtr**, int>)(&GetIids), 3u);
            AddMethod((delegate *unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(&GetRuntimeClassName), 4u);
            AddMethod((delegate *unmanaged[Stdcall]<IntPtr, int*, int>)(&GetTrustLevel), 5u);
#else
        AddMethod(new GetIidsDelegate(GetIids), 3u);
        AddMethod(new GetRuntimeClassNameDelegate(GetRuntimeClassName), 4u);
        AddMethod(new GetTrustLevelDelegate(GetTrustLevel), 5u);
#endif
    }

    /// <unmanaged>
    /// HRESULT STDMETHODCALLTYPE GetIids(
    ///   /* [out] */ __RPC__out ULONG *iidCount,
    ///   /* [size_is][size_is][out] */ __RPC__deref_out_ecount_full_opt(*iidCount) IID **iids
    /// )
    /// </unmanaged>
#if !NET5_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetIidsDelegate(IntPtr thisPtr, int* iidCount, IntPtr** iids);
#else
        [UnmanagedCallersOnly(CallConvs = new[]{typeof(CallConvStdcall)})]
#endif
    private static int GetIids(IntPtr thisPtr, int* iidCount, IntPtr** iids)
    {
        var @this = ToCallback<CallbackBase>(thisPtr);
        try
        {
            var container = @this.Shadow;

            var countGuids = container.Guids.Length;
            var iidsMemory = Marshal.AllocCoTaskMem(IntPtr.Size * countGuids);

            // Copy GUIDs deduced from Callback
            *iids = (IntPtr*) iidsMemory;
            *iidCount = countGuids;

            MemoryHelpers.CopyMemory(iidsMemory, new ReadOnlySpan<IntPtr>(container.Guids));

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
#if !NET5_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetRuntimeClassNameDelegate(IntPtr thisPtr, IntPtr* className);
#else
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
    private static int GetRuntimeClassName(IntPtr thisPtr, IntPtr* className)
    {
        var @this = ToCallback<IInspectable>(thisPtr);
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
#if !NET5_0_OR_GREATER
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetTrustLevelDelegate(IntPtr thisPtr, int* trustLevel);
#else
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
    private static int GetTrustLevel(IntPtr thisPtr, int* trustLevel)
    {
        var @this = ToCallback<IInspectable>(thisPtr);
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