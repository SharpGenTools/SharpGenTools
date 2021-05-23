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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpGen.Runtime
{
    /// <summary>
    /// A fast method to pass array of <see cref="CppObject"/>-derived objects to SharpGen methods.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="CppObject"/></typeparam>
    [DebuggerTypeProxy(typeof(InterfaceArray<>.InterfaceArrayDebugView))]
    [DebuggerDisplay("Count={values.Length}")]
    public sealed class InterfaceArray<T>: DisposeBase, IEnumerable<T> where T : CppObject
    {
        private T[] values;

        public InterfaceArray(params T[] array)
        {
            values = array;
            NativePointer = IntPtr.Zero;
            if (values != null)
            {
                var length = array.Length;
                values = new T[length];
                NativePointer = MemoryHelpers.AllocateMemory(length * IntPtr.Size);
                for (int i = 0; i < length; i++)
                    this[i] = array[i];
            }
        }

        public InterfaceArray(int size)
        {
            values = new T[size];
            NativePointer = MemoryHelpers.AllocateMemory(size * IntPtr.Size);
        }

        /// <summary>
        /// Gets the pointer to the native array associated to this instance.
        /// </summary>
        public IntPtr NativePointer { get; private set; }

        public int Length => values?.Length ?? 0;

        internal unsafe void SetFromNative(int index, T value)
        {
            values[index] = value;
            value.NativePointer = ((IntPtr*) NativePointer)[index];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                values = null;
            }
            MemoryHelpers.FreeMemory(NativePointer);
            NativePointer = IntPtr.Zero;
        }

        /// <summary>
        /// Gets or sets the <see cref="T"/> with the specified i.
        /// </summary>
        public unsafe T this[int i]
        {
            get => values[i];
            set
            {
                values[i] = value;
                ((IntPtr*) NativePointer)[i] = value?.NativePointer ?? IntPtr.Zero;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => new ArrayEnumerator(values.GetEnumerator());

        private readonly struct ArrayEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator enumerator;

            public ArrayEnumerator(IEnumerator enumerator) => this.enumerator = enumerator;

            public void Dispose()
            {
            }

            public bool MoveNext() => enumerator.MoveNext();
            public void Reset() => enumerator.Reset();
            public T Current => (T)enumerator.Current;
            object IEnumerator.Current => Current;
        }

        private sealed class InterfaceArrayDebugView
        {
            public InterfaceArrayDebugView(T[] array) => Items = array;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items { get; }
        }
    }
}