using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static partial class BooleanHelpers
    {
        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<byte> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (byte) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<short> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (short) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<int> dest) =>
            MemoryMarshal.Cast<RawBool, int>(array).CopyTo(dest);

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<long> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<sbyte> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (sbyte) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<ushort> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (ushort) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<uint> dest) =>
            MemoryMarshal.Cast<RawBool, uint>(array).CopyTo(dest);

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, Span<ulong> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (ulong) (array[i] ? 1 : 0);
        }
    }
}