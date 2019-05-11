using System;
using System.Collections.Generic;
using System.Text;

#if WIN
using NativeType = System.IntPtr;
#elif UNIX
using NativeType = System.Int32;
#else
using NativeType = System.IntPtr;
#endif

namespace SharpGen.Runtime
{
    public readonly struct NativeLong : IEquatable<NativeLong>
    {
        private readonly NativeType _value;

        public NativeLong(int value)
        {
            _value = (NativeType)value;
        }

        public NativeLong(long value)
        {
            _value = (NativeType)value;
        }

        public NativeLong(IntPtr value)
        {
            _value = (NativeType)value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return _value.Equals(obj);
        }

        public bool Equals(NativeLong other)
        {
            return _value.Equals(other._value);
        }

        private long ToInt64() => (long)_value;

        /// <summary>
        ///   Adds two sizes.
        /// </summary>
        /// <param name = "left">The first size to add.</param>
        /// <param name = "right">The second size to add.</param>
        /// <returns>The sum of the two sizes.</returns>
        public static NativeLong operator +(NativeLong left, NativeLong right)
        {
            return new NativeLong(left.ToInt64() + right.ToInt64());
        }

        /// <summary>
        ///   Assert a size (return it unchanged).
        /// </summary>
        /// <param name = "value">The size to assert (unchanged).</param>
        /// <returns>The asserted (unchanged) size.</returns>
        public static NativeLong operator +(NativeLong value)
        {
            return value;
        }

        /// <summary>
        ///   Subtracts two sizes.
        /// </summary>
        /// <param name = "left">The first size to subtract.</param>
        /// <param name = "right">The second size to subtract.</param>
        /// <returns>The difference of the two sizes.</returns>
        public static NativeLong operator -(NativeLong left, NativeLong right)
        {
            return new NativeLong(left.ToInt64() - right.ToInt64());
        }

        /// <summary>
        ///   Reverses the direction of a given size.
        /// </summary>
        /// <param name = "value">The size to negate.</param>
        /// <returns>A size facing in the opposite direction.</returns>
        public static NativeLong operator -(NativeLong value)
        {
            return new NativeLong(-value.ToInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeLong operator *(int scale, NativeLong value)
        {
            return new NativeLong(scale * value.ToInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeLong operator *(NativeLong value, int scale)
        {
            return new NativeLong(scale * value.ToInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeLong operator /(NativeLong value, int scale)
        {
            return new NativeLong(value.ToInt64() / scale);
        }

        /// <summary>
        ///   Tests for equality between two objects.
        /// </summary>
        /// <param name = "left">The first value to compare.</param>
        /// <param name = "right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
        public static bool operator ==(NativeLong left, NativeLong right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///   Tests for inequality between two objects.
        /// </summary>
        /// <param name = "left">The first value to compare.</param>
        /// <param name = "right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
        public static bool operator !=(NativeLong left, NativeLong right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeLong" /> to <see cref = "int" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator int(NativeLong value)
        {
            return (int)value._value;
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeLong" /> to <see cref = "long" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator long(NativeLong value)
        {
            return value.ToInt64();
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeLong" /> to <see cref = "long" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator IntPtr(NativeLong value)
        {
            return (IntPtr)value.ToInt64();
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeLong" /> from <see cref = "int" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator NativeLong(int value)
        {
            return new NativeLong(value);
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeLong" /> from <see cref = "long" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator NativeLong(long value)
        {
            return new NativeLong(value);
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeLong" /> from <see cref = "IntPtr" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator NativeLong(IntPtr value)
        {
            return new NativeLong(value);
        }
    }
}
