using System;
using System.Collections.Generic;
using System.Text;

#if WIN
using NativeType = System.UIntPtr;
#elif UNIX
using NativeType = System.UInt32;
#else
using NativeType = System.UIntPtr;
#endif

namespace SharpGen.Runtime
{
    public readonly struct NativeULong : IEquatable<NativeULong>
    {
        private readonly NativeType _value;

        public NativeULong(uint value)
        {
            _value = (NativeType)value;
        }

        public NativeULong(ulong value)
        {
            _value = (NativeType)value;
        }

        public NativeULong(UIntPtr value)
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

        public bool Equals(NativeULong other)
        {
            return _value.Equals(other._value);
        }

        private ulong ToUInt64() => (ulong)_value;

        /// <summary>
        ///   Adds two sizes.
        /// </summary>
        /// <param name = "left">The first size to add.</param>
        /// <param name = "right">The second size to add.</param>
        /// <returns>The sum of the two sizes.</returns>
        public static NativeULong operator +(NativeULong left, NativeULong right)
        {
            return new NativeULong(left.ToUInt64() + right.ToUInt64());
        }

        /// <summary>
        ///   Assert a size (return it unchanged).
        /// </summary>
        /// <param name = "value">The size to assert (unchanged).</param>
        /// <returns>The asserted (unchanged) size.</returns>
        public static NativeULong operator +(NativeULong value)
        {
            return value;
        }

        /// <summary>
        ///   Subtracts two sizes.
        /// </summary>
        /// <param name = "left">The first size to subtract.</param>
        /// <param name = "right">The second size to subtract.</param>
        /// <returns>The difference of the two sizes.</returns>
        public static NativeULong operator -(NativeULong left, NativeULong right)
        {
            return new NativeULong(left.ToUInt64() - right.ToUInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeULong operator *(uint scale, NativeULong value)
        {
            return new NativeULong(scale * value.ToUInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeULong operator *(NativeULong value, uint scale)
        {
            return new NativeULong(scale * value.ToUInt64());
        }

        /// <summary>
        ///   Scales a size by the given value.
        /// </summary>
        /// <param name = "value">The size to scale.</param>
        /// <param name = "scale">The amount by which to scale the size.</param>
        /// <returns>The scaled size.</returns>
        public static NativeULong operator /(NativeULong value, uint scale)
        {
            return new NativeULong(value.ToUInt64() / scale);
        }

        /// <summary>
        ///   Tests for equality between two objects.
        /// </summary>
        /// <param name = "left">The first value to compare.</param>
        /// <param name = "right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
        public static bool operator ==(NativeULong left, NativeULong right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///   Tests for inequality between two objects.
        /// </summary>
        /// <param name = "left">The first value to compare.</param>
        /// <param name = "right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
        public static bool operator !=(NativeULong left, NativeULong right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeULong" /> to <see cref = "uint" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator uint(NativeULong value)
        {
            return (uint)value._value;
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeULong" /> to <see cref = "ulong" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ulong(NativeULong value)
        {
            return value.ToUInt64();
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "NativeULong" /> to <see cref = "UIntPtr" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator UIntPtr(NativeULong value)
        {
            return (UIntPtr)value.ToUInt64();
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeULong" /> from <see cref = "uint" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator NativeULong(uint value)
        {
            return new NativeULong(value);
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeULong" /> from <see cref = "ulong" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator NativeULong(ulong value)
        {
            return new NativeULong(value);
        }

        /// <summary>
        ///   Performs an implicit conversion to <see cref = "NativeULong" /> from <see cref = "UIntPtr" />.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator NativeULong(UIntPtr value)
        {
            return new NativeULong(value);
        }
    }
}
