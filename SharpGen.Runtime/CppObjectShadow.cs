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

namespace SharpGen.Runtime
{
    /// <summary>
    /// An Interface shadow callback
    /// </summary>
    public abstract unsafe class CppObjectShadow : CppObject
    {
        /// <summary>
        /// Gets the callback.
        /// </summary>
        public ICallbackable Callback { get; private set; }

        /// <summary>
        /// Gets the VTBL associated with this shadow instance.
        /// </summary>
        protected abstract CppObjectVtbl Vtbl { get; }

        /// <summary>
        /// Initializes the specified shadow instance from a vtbl and a callback.
        /// </summary>
        /// <param name="callbackInstance">The callback.</param>
        public virtual void Initialize(ICallbackable callbackInstance)
        {
            Callback = callbackInstance;

            Debug.Assert(Marshal.SizeOf(typeof(CppObjectNative)) == CppObjectNative.Size);

            // Allocate ptr to vtbl + ptr to callback together
            var nativePointer = Marshal.AllocHGlobal(CppObjectNative.Size);
            ref var native = ref *(CppObjectNative*)nativePointer;

            native.VtblPointer = Vtbl.Pointer;
            native.Shadow = GCHandle.Alloc(this);

            NativePointer = nativePointer;
        }

        protected override void DisposeCore(IntPtr nativePointer, bool disposing)
        {
            Callback = null;

            // Free the GCHandle
            ((CppObjectNative*) nativePointer)->Shadow.Free();

            // Free instance
            Marshal.FreeHGlobal(nativePointer);
        }

        protected internal static T ToShadow<T>(IntPtr thisPtr) where T : CppObjectShadow
        {
            var target = ((CppObjectNative*) thisPtr)->Shadow.Target;
            return (T) target;
        }

        private ref struct CppObjectNative
        {
            internal static readonly int Size = IntPtr.Size * 2;

            // ReSharper disable once NotAccessedField.Local
            public IntPtr VtblPointer;
            private IntPtr shadowPointer;

            public GCHandle Shadow
            {
                readonly get => GCHandle.FromIntPtr(shadowPointer);
                set => shadowPointer = GCHandle.ToIntPtr(value);
            }
        }
    }
}
