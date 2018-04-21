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
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    /// <summary>
    /// A fast method to pass array of <see cref="CppObject"/>-derived objects to SharpGen methods.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="CppObject"/></typeparam>
    public class InterfaceArray<T>: DisposeBase, IEnumerable<T>
        where T : CppObject
    {
        protected T[] values;
        private IntPtr nativeBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceArray"/> class.
        /// </summary>
        /// <param name="array">The array.</param>
        public unsafe InterfaceArray(params T[] array)
        {
            values = array;
            nativeBuffer = IntPtr.Zero;
            if (values != null)
            {
                var length = array.Length;
                values = new T[length];
                nativeBuffer = MemoryHelpers.AllocateMemory(length * sizeof(IntPtr));
                for (int i = 0; i < length; i++)
                    Set(i, array[i]);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceArray"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public unsafe InterfaceArray(int size)
        {
            values = new T[size];
            nativeBuffer = MemoryHelpers.AllocateMemory(size * sizeof(IntPtr));
        }

        /// <summary>
        /// Gets the pointer to the native array associated to this instance.
        /// </summary>
        public IntPtr NativePointer
        {
            get
            {
                return nativeBuffer;
            }
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        public int Length
        {
            get
            {
                return values == null ? 0 : values.Length;
            }
        }

        /// <summary>
        /// Gets an object at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>A <see cref="ComObject"/></returns>
        public CppObject Get(int index)
        {
            return values[index];
        }

        internal void SetFromNative(int index, T value)
        {
            values[index] = value;
            unsafe
            {
                value.NativePointer = ((IntPtr*)nativeBuffer)[index];
            }
        }

        /// <summary>
        /// Sets an object at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public void Set(int index, T value)
        {
            values[index] = value;
            unsafe
            {
                ((IntPtr*)nativeBuffer)[index] = value?.NativePointer ?? IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                values = null;
            }
            MemoryHelpers.FreeMemory(nativeBuffer);
            nativeBuffer = IntPtr.Zero;
        }

        /// <summary>
        /// Gets or sets the <see cref="T"/> with the specified i.
        /// </summary>
        public T this[int i]
        {
            get
            {
                return (T)Get(i);
            }
            set
            {
                Set(i, value);
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArrayEnumerator(values.GetEnumerator());
        }

        private struct ArrayEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator enumerator;

            public ArrayEnumerator(IEnumerator enumerator)
            {
                this.enumerator = enumerator;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }

            public T Current
            {
                get
                {
                    return (T)enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    }
}