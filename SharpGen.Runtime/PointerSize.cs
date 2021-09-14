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
using System.Globalization;

namespace SharpGen.Runtime
{
    /// <summary>
    ///   The maximum number of bytes to which a pointer can point. Use for a count that must span the full range of a pointer.
    ///   Equivalent to the native type size_t.
    /// </summary>
    public readonly struct PointerSize : IEquatable<PointerSize>, IFormattable
    {
        private readonly IntPtr _size;

        /// <summary>
        /// An empty pointer size initialized to zero.
        /// </summary>
        public static readonly PointerSize Zero = new(0);

        public PointerSize(IntPtr size) => _size = size;
        private unsafe PointerSize(void* size) => _size = new IntPtr(size);
        public PointerSize(int size) => _size = new IntPtr(size);
        public PointerSize(long size) => _size = new IntPtr(size);

        public override string ToString() => ToString(null, null);

        public string ToString(string format, IFormatProvider formatProvider) => string.Format(
            formatProvider ?? CultureInfo.CurrentCulture,
            string.IsNullOrEmpty(format) ? "{0}" : "{0:" + format + "}",
            _size
        );

        public string ToString(string format) => ToString(format, null);

        public override int GetHashCode() => _size.GetHashCode();

        public bool Equals(PointerSize other) => _size.Equals(other._size);

        public override bool Equals(object value)
        {
            if (ReferenceEquals(null, value)) return false;
            return value is PointerSize size && Equals(size);
        }

        public static PointerSize operator +(PointerSize left, PointerSize right) => new(left._size.ToInt64() + right._size.ToInt64());
        public static PointerSize operator +(PointerSize value) => value;
        public static PointerSize operator -(PointerSize left, PointerSize right) => new(left._size.ToInt64() - right._size.ToInt64());
        public static PointerSize operator -(PointerSize value) => new(-value._size.ToInt64());
        public static PointerSize operator *(int scale, PointerSize value) => new(scale*value._size.ToInt64());
        public static PointerSize operator *(PointerSize value, int scale) => new(scale*value._size.ToInt64());
        public static PointerSize operator /(PointerSize value, int scale) => new(value._size.ToInt64()/scale);
        public static bool operator ==(PointerSize left, PointerSize right) => left.Equals(right);
        public static bool operator !=(PointerSize left, PointerSize right) => !left.Equals(right);
        public static implicit operator int(PointerSize value) => value._size.ToInt32();
        public static implicit operator long(PointerSize value) => value._size.ToInt64();
        public static implicit operator PointerSize(int value) => new(value);
        public static implicit operator PointerSize(long value) => new(value);
        public static implicit operator PointerSize(IntPtr value) => new(value);
        public static implicit operator IntPtr(PointerSize value) => value._size;
        public static unsafe implicit operator PointerSize(void* value) => new(value);
        public static unsafe implicit operator void*(PointerSize value) => (void*) value._size;
    }
}