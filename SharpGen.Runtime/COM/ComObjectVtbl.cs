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

namespace SharpGen.Runtime
{
    [DebuggerTypeProxy(typeof(CppObjectVtblDebugView))]
    public unsafe class ComObjectVtbl : CppObjectVtbl
    {
        public ComObjectVtbl(int numberOfCallbackMethods) : base(numberOfCallbackMethods + 3)
        {
#if NET5_0_OR_GREATER
            AddMethod((delegate* unmanaged[Stdcall]<IntPtr, Guid*, void*, int>)(&QueryInterfaceImpl), 0u);
            AddMethod((delegate* unmanaged[Stdcall]<IntPtr, uint>)(&AddRefImpl), 1u);
            AddMethod((delegate* unmanaged[Stdcall]<IntPtr, uint>)(&ReleaseImpl), 2u);
#else
            AddMethod(new QueryInterfaceDelegate(QueryInterfaceImpl), 0u);
            AddMethod(new AddRefDelegate(AddRefImpl), 1u);
            AddMethod(new ReleaseDelegate(ReleaseImpl), 2u);
#endif
        }

#if !NET5_0_OR_GREATER
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int QueryInterfaceDelegate(IntPtr thisObject, Guid* guid, void* output);
#else
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static int QueryInterfaceImpl(IntPtr thisObject, Guid* guid, void* output)
        {
            var callback = ToCallback<CallbackBase>(thisObject);
#if DEBUG
            try
            {
#endif
                ref var ppvObject = ref Unsafe.AsRef<IntPtr>(output);
                var result = callback.Shadow.Find(*guid);
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

#if !NET5_0_OR_GREATER
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint AddRefDelegate(IntPtr thisObject);
#else
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static uint AddRefImpl(IntPtr thisObject) =>
            MarshallingHelpers.AddRef(ToCallback<IUnknown>(thisObject));

#if !NET5_0_OR_GREATER
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ReleaseDelegate(IntPtr thisObject);
#else
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static uint ReleaseImpl(IntPtr thisObject) =>
            MarshallingHelpers.Release(ToCallback<IUnknown>(thisObject));
    }
}