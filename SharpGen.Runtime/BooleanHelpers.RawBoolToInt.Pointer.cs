using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    public static unsafe partial class BooleanHelpers
    {
        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, byte* dest)
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
        public static void ConvertToIntArray(Span<RawBool> array, short* dest)
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
        public static void ConvertToIntArray(Span<RawBool> array, int* dest)
        {
            fixed (void* src = array)
                Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, long* dest)
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
        public static void ConvertToIntArray(Span<RawBool> array, sbyte* dest)
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
        public static void ConvertToIntArray(Span<RawBool> array, ushort* dest)
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
        public static void ConvertToIntArray(Span<RawBool> array, uint* dest)
        {
            fixed (void* src = array)
                Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
        }

        /// <summary>
        /// Converts <see cref="RawBool"/> array to an array of integers.
        /// </summary>
        /// <param name="array">The <see cref="RawBool"/> array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<RawBool> array, ulong* dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (ulong) (array[i] ? 1 : 0);
        }
    }
}