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
using System.Diagnostics.CodeAnalysis;

namespace SharpGen.Runtime;

/// <summary>
/// A fast method to pass array of <see cref="CppObject"/>-derived objects to SharpGen methods.
/// </summary>
/// <typeparam name="T">Type of the <see cref="CppObject"/></typeparam>
[DebuggerTypeProxy(typeof(InterfaceArray<>.InterfaceArrayDebugView))]
[DebuggerDisplay("Count={" + nameof(Length) + "}")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
public unsafe struct InterfaceArray<
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
T> : IReadOnlyList<T>, IEnlightenedDisposable, IDisposable
    where T : CppObject
{
    // .NET Native has issues with <...> in property backing fields in structs
    private T[] _values;
    private void* _nativePointer;

    public InterfaceArray(params T[] array)
    {
        if (array != null)
        {
            var length = unchecked((uint) array.Length);
            if (length != 0)
            {
                _values = new T[length];
                _nativePointer = MemoryHelpers.AllocateMemory(length * (uint)IntPtr.Size);
                for (var i = 0; i < length; i++)
                    this[i] = array[i];
            }
            else
            {
                _nativePointer = default;
                _values = null;
            }
        }
        else
        {
            _nativePointer = default;
            _values = null;
        }
    }

    public InterfaceArray(int size) : this((uint) size)
    {
    }

    public InterfaceArray(uint size)
    {
        if (size > 0)
        {
            _values = new T[size];
            _nativePointer = MemoryHelpers.AllocateMemory(size * (uint)IntPtr.Size);
        }
        else
        {
            _nativePointer = default;
            _values = null;
        }
    }

    public InterfaceArray(void* pointer, nuint size)
    {
        if (size > 0)
        {
            _nativePointer = pointer;
            _values = new T[size];
            var ptr = (IntPtr*) pointer;
            for (nuint i = 0; i < size; i++)
                _values[i] = MarshallingHelpers.FromPointer<T>(ptr[i]);
        }
        else
        {
            _nativePointer = default;
            _values = null;
        }
    }

    /// <summary>
    /// Gets the pointer to the native array associated to this instance.
    /// </summary>
    public void* NativePointer
    {
        readonly get => _nativePointer;
        private set => _nativePointer = value;
    }

    public readonly int Length => _values?.Length ?? 0;

    private readonly bool IsDisposed => NativePointer == default;

    public void CheckAndDispose(bool disposing)
    {
        if (IsDisposed)
            return;

        _values = null;

        MemoryHelpers.FreeMemory(NativePointer);
        NativePointer = default;
    }

    public void Dispose() => CheckAndDispose(true);

    public readonly T this[int i]
    {
        get => _values[i];
        set
        {
            _values[i] = value;
            ((IntPtr*) NativePointer)[i] = value?.NativePointer ?? IntPtr.Zero;
        }
    }

    public readonly T this[uint i]
    {
        get => _values[i];
        set
        {
            _values[i] = value;
            ((IntPtr*) NativePointer)[i] = value?.NativePointer ?? IntPtr.Zero;
        }
    }

    public readonly T this[nint i]
    {
        get => _values[i];
        set
        {
            _values[i] = value;
            ((IntPtr*) NativePointer)[i] = value?.NativePointer ?? IntPtr.Zero;
        }
    }

    public readonly T this[nuint i]
    {
        get => _values[i];
        set
        {
            _values[i] = value;
            ((IntPtr*) NativePointer)[i] = value?.NativePointer ?? IntPtr.Zero;
        }
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => _values != null ? _values.GetEnumerator() : EmptyEnumerator.Instance;

    public readonly IEnumerator<T> GetEnumerator() =>
        _values != null ? new ArrayEnumerator(_values.GetEnumerator()) : EmptyEnumerator.Instance;

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

    private readonly struct EmptyEnumerator : IEnumerator<T>
    {
        public static readonly IEnumerator<T> Instance = default(EmptyEnumerator);

        public void Dispose()
        {
        }

        public bool MoveNext() => false;

        public void Reset()
        {
        }

        public T Current => throw new InvalidOperationException();
        object IEnumerator.Current => Current;
    }

    private sealed class InterfaceArrayDebugView
    {
        public InterfaceArrayDebugView(InterfaceArray<T> array) => Items = array._values;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items { get; }
    }

    readonly int IReadOnlyCollection<T>.Count => Length;
}