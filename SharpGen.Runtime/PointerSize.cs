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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SharpGen.Runtime;

/// <summary>
///   The maximum number of bytes to which a pointer can point. Use for a count that must span the full range of a pointer.
///   Equivalent to the native type size_t.
/// </summary>
public readonly struct PointerSize : IEquatable<PointerSize>,
#if NET7_0_OR_GREATER
    IComparable<PointerSize>,
#endif
    IFormattable
{
    public readonly IntPtr Value;

    /// <summary>
    /// An empty pointer size initialized to zero.
    /// </summary>
    public static readonly PointerSize Zero = new(0);

    public PointerSize(IntPtr value) => Value = value;
    private unsafe PointerSize(void* value) => Value = new IntPtr(value);
    public PointerSize(int value) => Value = new IntPtr(value);
    public PointerSize(long value) => Value = new IntPtr(value);

    public override string ToString() => ToString(null, null);

    public string ToString(string format, IFormatProvider formatProvider) => string.Format(
        formatProvider ?? CultureInfo.CurrentCulture,
        string.IsNullOrEmpty(format) ? "{0}" : "{0:" + format + "}",
        Value
    );

    public string ToString(string format) => ToString(format, null);

    public override int GetHashCode() => Value.GetHashCode();

    public bool Equals(PointerSize other) => Value.Equals(other.Value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PointerSize value && Equals(value);

#if NET7_0_OR_GREATER
    public int CompareTo(object? obj)
    {
        if (obj is PointerSize other)
        {
            return CompareTo(other);
        }

        return (obj is null) ? 1 : throw new ArgumentException("obj is not an instance of PointerSize.");
    }

    public int CompareTo(PointerSize other) => Value.CompareTo(other.Value);
#endif

    public static PointerSize operator +(PointerSize left, PointerSize right) => new(left.Value.ToInt64() + right.Value.ToInt64());
    public static PointerSize operator +(PointerSize value) => value;
    public static PointerSize operator -(PointerSize left, PointerSize right) => new(left.Value.ToInt64() - right.Value.ToInt64());
    public static PointerSize operator -(PointerSize value) => new(-value.Value.ToInt64());
    public static PointerSize operator *(int scale, PointerSize value) => new(scale * value.Value.ToInt64());
    public static PointerSize operator *(PointerSize value, int scale) => new(scale * value.Value.ToInt64());
    public static PointerSize operator /(PointerSize value, int scale) => new(value.Value.ToInt64() / scale);
    public static bool operator ==(PointerSize left, PointerSize right) => left.Equals(right);
    public static bool operator !=(PointerSize left, PointerSize right) => !left.Equals(right);
    public static implicit operator int(PointerSize value) => value.Value.ToInt32();
    public static implicit operator long(PointerSize value) => value.Value.ToInt64();
    public static implicit operator PointerSize(int value) => new(value);
    public static implicit operator PointerSize(long value) => new(value);
    public static implicit operator PointerSize(IntPtr value) => new(value);
    public static implicit operator IntPtr(PointerSize value) => value.Value;
    public static unsafe implicit operator PointerSize(void* value) => new(value);
    public static unsafe implicit operator void*(PointerSize value) => (void*) value.Value;
}