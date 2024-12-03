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

#nullable enable

namespace SharpGen.Runtime;

/// <summary>
///   The maximum number of bytes to which a pointer can point. Use for a count that must span the full range of a pointer.
///   Equivalent to the native type size_t.
/// </summary>
public readonly struct PointerUSize : IEquatable<PointerUSize>,
#if NET8_0_OR_GREATER
    IComparable<PointerUSize>,
#endif
    IFormattable
{
    public readonly UIntPtr Value;

    /// <summary>
    /// An empty pointer size initialized to zero.
    /// </summary>
    public static readonly PointerUSize Zero = new(0);

    public PointerUSize(UIntPtr value) => Value = value;
    private unsafe PointerUSize(void* value) => Value = new UIntPtr(value);
    public PointerUSize(uint value) => Value = new UIntPtr(value);
    public PointerUSize(ulong value) => Value = new UIntPtr(value);

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) => string.Format(
        formatProvider ?? CultureInfo.CurrentCulture,
        string.IsNullOrEmpty(format) ? "{0}" : "{0:" + format + "}",
        Value
    );

    public string ToString(string? format) => ToString(format, null);

    public override int GetHashCode() => Value.GetHashCode();

    public bool Equals(PointerUSize other) => Value.Equals(other.Value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is PointerUSize value && Equals(value);


#if NET8_0_OR_GREATER
    public int CompareTo(object? obj)
    {
        if (obj is PointerUSize other)
        {
            return CompareTo(other);
        }

        return (obj is null) ? 1 : throw new ArgumentException("obj is not an instance of PointerUSize.");
    }

    public int CompareTo(PointerUSize other) => Value.CompareTo(other.Value);
#endif

    public static PointerUSize operator +(PointerUSize left, PointerUSize right) => new(left.Value.ToUInt64() + right.Value.ToUInt64());
    public static PointerUSize operator -(PointerUSize left, PointerUSize right) => new(left.Value.ToUInt64() - right.Value.ToUInt64());
    public static PointerUSize operator *(uint scale, PointerUSize value) => new(scale*value.Value.ToUInt64());
    public static PointerUSize operator *(PointerUSize value, uint scale) => new(scale*value.Value.ToUInt64());
    public static PointerUSize operator /(PointerUSize value, uint scale) => new(value.Value.ToUInt64()/scale);
    public static bool operator ==(PointerUSize left, PointerUSize right) => left.Equals(right);
    public static bool operator !=(PointerUSize left, PointerUSize right) => !left.Equals(right);
    public static implicit operator uint(PointerUSize value) => value.Value.ToUInt32();
    public static implicit operator ulong(PointerUSize value) => value.Value.ToUInt64();
    public static implicit operator PointerUSize(uint value) => new(value);
    public static implicit operator PointerUSize(ulong value) => new(value);
    public static implicit operator PointerUSize(UIntPtr value) => new(value);
    public static implicit operator UIntPtr(PointerUSize value) => value.Value;
    public static unsafe implicit operator PointerUSize(void* value) => new(value);
    public static unsafe implicit operator void*(PointerUSize value) => (void*) value.Value;
}