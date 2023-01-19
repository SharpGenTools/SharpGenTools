using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

[StructLayout(LayoutKind.Explicit)]
public readonly struct NativeULong : IEquatable<NativeULong>, IComparable<NativeULong>, IComparable
#if NET6_0_OR_GREATER
, IFormattable
#endif
{
    [FieldOffset(0)]
    private readonly nuint _pointerValue;
    [FieldOffset(0)]
    private readonly uint _intValue;

    private static readonly bool UseInt = NativeLong.UseInt;

    public NativeULong(uint value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nuint) value;
        }
    }

    public NativeULong(ulong value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = (uint) value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nuint) value;
        }
    }

    public NativeULong(UIntPtr value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = (uint) value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nuint) value;
        }
    }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() =>
        UseInt ? _intValue.ToString() : _pointerValue.ToString();

#if NET6_0_OR_GREATER
    public string ToString(string format)
    {
        return UseInt ? _intValue.ToString(format) : _pointerValue.ToString(format);
    }

    public string ToString(IFormatProvider formatProvider)
    {
        return UseInt ? _intValue.ToString(formatProvider) : _pointerValue.ToString(formatProvider);
    }

    /// <inheritdoc cref="IFormattable.ToString(string,System.IFormatProvider)" />
    public string ToString(string format, IFormatProvider formatProvider)
    {
        return UseInt ? _intValue.ToString(format, formatProvider) : _pointerValue.ToString(format, formatProvider);
    }
#endif

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() =>
        UseInt ? _intValue.GetHashCode() : _pointerValue.GetHashCode();

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(NativeULong other) =>
        UseInt ? _intValue == other._intValue : _pointerValue.Equals(other._pointerValue);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object obj) =>
        obj switch
        {
            null => false,
            NativeULong other => Equals(other),
            uint other => Equals(new NativeULong(other)),
            ulong other => Equals(new NativeULong(other)),
            UIntPtr other => Equals(new NativeULong(other)),
            _ => false
        };

    public static NativeULong operator +(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue + right._intValue)
            : new NativeULong(left._pointerValue + right._pointerValue);

    public static NativeULong operator +(NativeULong value) => value;

    public static NativeULong operator -(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue - right._intValue)
            : new NativeULong(left._pointerValue - right._pointerValue);

    public static NativeULong operator *(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue * right._intValue)
            : new NativeULong(left._pointerValue * right._pointerValue);

    public static NativeULong operator /(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue / right._intValue)
            : new NativeULong(left._pointerValue / right._pointerValue);

    public static NativeULong operator %(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue % right._intValue)
            : new NativeULong(left._pointerValue % right._pointerValue);

    public static NativeULong operator >>(NativeULong left, int right) =>
        UseInt
            ? new NativeULong(left._intValue >> right)
            : new NativeULong(left._pointerValue >> right);

    public static NativeULong operator <<(NativeULong left, int right) =>
        UseInt
            ? new NativeULong(left._intValue << right)
            : new NativeULong(left._pointerValue << right);

    public static NativeULong operator &(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue & right._intValue)
            : new NativeULong(left._pointerValue & right._pointerValue);

    public static NativeULong operator |(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue | right._intValue)
            : new NativeULong(left._pointerValue | right._pointerValue);

    public static NativeULong operator ^(NativeULong left, NativeULong right) =>
        UseInt
            ? new NativeULong(left._intValue ^ right._intValue)
            : new NativeULong(left._pointerValue ^ right._pointerValue);

    public static NativeULong operator ~(NativeULong value) =>
        UseInt
            ? new NativeULong(~value._intValue)
            : new NativeULong(~value._pointerValue);

    /// <summary>
    ///   Tests for equality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator ==(NativeULong left, NativeULong right) => left.Equals(right);

    /// <summary>
    ///   Tests for inequality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator !=(NativeULong left, NativeULong right) => !left.Equals(right);

    /// <summary>
    ///   Performs an explicit conversion from <see cref = "NativeULong" /> to <see cref = "uint" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator uint(NativeULong value) =>
        UseInt ? value._intValue : (uint) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion from <see cref = "NativeULong" /> to <see cref = "ulong" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator ulong(NativeULong value) =>
        UseInt ? value._intValue : (ulong) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion from <see cref = "NativeULong" /> to <see cref = "UIntPtr" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator UIntPtr(NativeULong value) =>
        UseInt ? (UIntPtr) value._intValue : (UIntPtr) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion to <see cref = "NativeULong" /> from <see cref = "uint" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator NativeULong(uint value) => new NativeULong(value);

    /// <summary>
    ///   Performs an explicit conversion to <see cref = "NativeULong" /> from <see cref = "ulong" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator NativeULong(ulong value) => new NativeULong(value);

    /// <summary>
    ///   Performs an explicit conversion to <see cref = "NativeULong" /> from <see cref = "UIntPtr" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator NativeULong(UIntPtr value) => new NativeULong(value);

    /// <inheritdoc cref="IComparable.CompareTo" />
    public int CompareTo(NativeULong other)
    {
        if (UseInt)
            return _intValue.CompareTo(other._intValue);

#if !NET5_0
        return ((ulong) _pointerValue).CompareTo((ulong) other._pointerValue);
#else
            return _pointerValue.CompareTo(other._pointerValue);
#endif
    }

    /// <inheritdoc cref="IComparable.CompareTo" />
    public int CompareTo(object obj) =>
        obj switch
        {
            null => 1,
            NativeULong other => CompareTo(other),
            uint other => CompareTo(new NativeULong(other)),
            ulong other => CompareTo(new NativeULong(other)),
            UIntPtr other => CompareTo(new NativeULong(other)),
            _ => throw new ArgumentException($"Object must be convertible to {nameof(NativeULong)} type")
        };

    public static bool operator <(NativeULong left, NativeULong right) => left.CompareTo(right) < 0;

    public static bool operator >(NativeULong left, NativeULong right) => left.CompareTo(right) > 0;

    public static bool operator <=(NativeULong left, NativeULong right) => left.CompareTo(right) <= 0;

    public static bool operator >=(NativeULong left, NativeULong right) => left.CompareTo(right) >= 0;
}