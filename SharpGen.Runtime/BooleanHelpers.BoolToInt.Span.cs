using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static partial class BooleanHelpers
    {
        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<byte> dest) =>
            MemoryMarshal.AsBytes(array).CopyTo(dest);

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<short> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (short) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<int> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<long> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = array[i] ? 1 : 0;
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<sbyte> dest) =>
            MemoryMarshal.AsBytes(array).CopyTo(MemoryMarshal.AsBytes(dest));

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<ushort> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (ushort) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<uint> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (uint) (array[i] ? 1 : 0);
        }

        /// <summary>
        /// Converts bool array to an array of integers.
        /// </summary>
        /// <param name="array">The bool array.</param>
        /// <param name="dest">The destination array of integers.</param>
        public static void ConvertToIntArray(Span<bool> array, Span<ulong> dest)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                dest[i] = (ulong) (array[i] ? 1 : 0);
        }
    }
}