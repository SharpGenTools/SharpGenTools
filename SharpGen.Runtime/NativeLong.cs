using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

[StructLayout(LayoutKind.Explicit)]
public readonly struct NativeLong : IEquatable<NativeLong>, IComparable<NativeLong>, IComparable
#if NET6_0_OR_GREATER
, IFormattable
#endif
{
    [FieldOffset(0)]
    private readonly nint _pointerValue;
    [FieldOffset(0)]
    private readonly int _intValue;

    // See https://en.cppreference.com/w/cpp/language/types
    internal static readonly bool UseInt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public NativeLong(int value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nint) value;
        }
    }

    public NativeLong(long value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = (int) value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nint) value;
        }
    }

    public NativeLong(IntPtr value)
    {
        if (UseInt)
        {
            _pointerValue = 0;
            _intValue = (int) value;
        }
        else
        {
            _intValue = 0;
            _pointerValue = (nint) value;
        }
    }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => 
        UseInt ? _intValue.ToString() : _pointerValue.ToString();

    public string ToString(string format) => 
        UseInt ? _intValue.ToString(format) : _pointerValue.ToString(format);

#if NET6_0_OR_GREATER
    public string ToString(IFormatProvider formatProvider) => 
            UseInt ? _intValue.ToString(formatProvider) : _pointerValue.ToString(formatProvider);

        /// <inheritdoc cref="IFormattable.ToString(string,System.IFormatProvider)" />
        public string ToString(string format, IFormatProvider formatProvider) => 
            UseInt ? _intValue.ToString(format, formatProvider) : _pointerValue.ToString(format, formatProvider);
#endif
        
    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 
        UseInt ? _intValue.GetHashCode() : _pointerValue.GetHashCode();

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(NativeLong other) => 
        UseInt ? _intValue == other._intValue : _pointerValue.Equals(other._pointerValue);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object obj) =>
        obj switch
        {
            null => false,
            NativeLong other => Equals(other),
            int other => Equals(new NativeLong(other)),
            long other => Equals(new NativeLong(other)),
            IntPtr other => Equals(new NativeLong(other)),
            _ => false
        };

    public static NativeLong operator +(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue + right._intValue)
            : new NativeLong(left._pointerValue + right._pointerValue);

    public static NativeLong operator +(NativeLong value) => value;

    public static NativeLong operator -(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue - right._intValue)
            : new NativeLong(left._pointerValue - right._pointerValue);

    public static NativeLong operator -(NativeLong value) =>
        UseInt
            ? new NativeLong(-value._intValue)
            : new NativeLong(-value._pointerValue);

    public static NativeLong operator *(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue * right._intValue)
            : new NativeLong(left._pointerValue * right._pointerValue);

    public static NativeLong operator /(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue / right._intValue)
            : new NativeLong(left._pointerValue / right._pointerValue);

    public static NativeLong operator %(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue % right._intValue)
            : new NativeLong(left._pointerValue % right._pointerValue);

    public static NativeLong operator >>(NativeLong left, int right) =>
        UseInt
            ? new NativeLong(left._intValue >> right)
            : new NativeLong(left._pointerValue >> right);

    public static NativeLong operator <<(NativeLong left, int right) =>
        UseInt
            ? new NativeLong(left._intValue << right)
            : new NativeLong(left._pointerValue << right);

    public static NativeLong operator &(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue & right._intValue)
            : new NativeLong(left._pointerValue & right._pointerValue);

    public static NativeLong operator |(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue | right._intValue)
            : new NativeLong(left._pointerValue | right._pointerValue);

    public static NativeLong operator ^(NativeLong left, NativeLong right) =>
        UseInt
            ? new NativeLong(left._intValue ^ right._intValue)
            : new NativeLong(left._pointerValue ^ right._pointerValue);

    public static NativeLong operator ~(NativeLong value) =>
        UseInt
            ? new NativeLong(~value._intValue)
            : new NativeLong(~value._pointerValue);

    /// <summary>
    ///   Tests for equality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator ==(NativeLong left, NativeLong right) => left.Equals(right);

    /// <summary>
    ///   Tests for inequality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator !=(NativeLong left, NativeLong right) => !left.Equals(right);

    /// <summary>
    ///   Performs an explicit conversion from <see cref = "NativeLong" /> to <see cref = "int" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator int(NativeLong value) => 
        UseInt ? value._intValue : (int) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion from <see cref = "NativeLong" /> to <see cref = "long" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator long(NativeLong value) => 
        UseInt ? value._intValue : (long) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion from <see cref = "NativeLong" /> to <see cref = "long" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator IntPtr(NativeLong value) => 
        UseInt ? (IntPtr) value._intValue : (IntPtr) value._pointerValue;

    /// <summary>
    ///   Performs an implicit conversion to <see cref = "NativeLong" /> from <see cref = "int" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator NativeLong(int value) => new NativeLong(value);

    /// <summary>
    ///   Performs an explicit conversion to <see cref = "NativeLong" /> from <see cref = "long" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator NativeLong(long value) => new NativeLong(value);

    /// <summary>
    ///   Performs an explicit conversion to <see cref = "NativeLong" /> from <see cref = "IntPtr" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator NativeLong(IntPtr value) => new NativeLong(value);

    /// <inheritdoc cref="IComparable.CompareTo" />
    public int CompareTo(NativeLong other)
    {
        if (UseInt)
            return _intValue.CompareTo(other._intValue);

#if !NET5_0
        return ((long)_pointerValue).CompareTo((long) other._pointerValue);
#else
            return _pointerValue.CompareTo(other._pointerValue);
#endif
    }

    /// <inheritdoc cref="IComparable.CompareTo" />
    public int CompareTo(object obj) =>
        obj switch
        {
            null => 1,
            NativeLong other => CompareTo(other),
            int other => CompareTo(new NativeLong(other)),
            long other => CompareTo(new NativeLong(other)),
            IntPtr other => CompareTo(new NativeLong(other)),
            _ => throw new ArgumentException($"Object must be convertible to {nameof(NativeLong)} type")
        };

    public static bool operator <(NativeLong left, NativeLong right) => left.CompareTo(right) < 0;

    public static bool operator >(NativeLong left, NativeLong right) => left.CompareTo(right) > 0;

    public static bool operator <=(NativeLong left, NativeLong right) => left.CompareTo(right) <= 0;

    public static bool operator >=(NativeLong left, NativeLong right) => left.CompareTo(right) >= 0;
}