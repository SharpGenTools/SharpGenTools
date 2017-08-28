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
using System.Runtime.InteropServices;
using System.Threading;

namespace SharpGen.Runtime
{
    /// <summary>
    /// A COM Interface Callback
    /// </summary>
    internal abstract class ComObjectShadow : CppObjectShadow
    {
        public static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        internal class ComObjectVtbl : CppObjectVtbl
        {
            public ComObjectVtbl(int numberOfCallbackMethods)
                : base(numberOfCallbackMethods + 3)
            {
                unsafe
                {
                    AddMethod(new QueryInterfaceDelegate(QueryInterfaceImpl));
                    AddMethod(new AddRefDelegate(AddRefImpl));
                    AddMethod(new ReleaseDelegate(ReleaseImpl));
                }
            }

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate int QueryInterfaceDelegate(IntPtr thisObject, IntPtr guid, out IntPtr output);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate uint AddRefDelegate(IntPtr thisObject);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate uint ReleaseDelegate(IntPtr thisObject);

            protected static unsafe int QueryInterfaceImpl(IntPtr thisObject, IntPtr guidPtr, out IntPtr output)
            {
                Guid guid = *((Guid*)guidPtr);
                var shadow = ToShadow<ComObjectShadow>(thisObject);
                if (shadow == null)
                {
                    output = IntPtr.Zero;
                    return Result.NoInterface.Code;
                }

                var callback = (IUnknown)shadow.Callback;
                callback.QueryInterface(guid, out output);
                return (int)(output == IntPtr.Zero ? Result.NoInterface : Result.Ok);
            }

            protected static uint AddRefImpl(IntPtr thisObject)
            {
                var shadow = ToShadow<ComObjectShadow>(thisObject);
                // The shadow could be null if it is released explicitly
                // But we are callbacked by a C++ that want to release it.
                if (shadow == null)
                    return 0;

                var callback = (IUnknown)shadow.Callback;
                return callback.AddRef();
            }

            protected static uint ReleaseImpl(IntPtr thisObject)
            {
                var shadow = ToShadow<ComObjectShadow>(thisObject);
                // The shadow could be null if it is released explicitly
                // But we are callbacked by a C++ that want to release it.
                if (shadow == null)
                    return 0;

                var callback = (IUnknown)shadow.Callback;
                return callback.Release();
            }
        }
    }
}