using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime
{
    public static partial class BooleanHelpers
    {
        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<byte> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<short> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<int> src, Span<RawBool> array) =>
            MemoryMarshal.Cast<int, RawBool>(src).CopyTo(array);

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<long> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<sbyte> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<ushort> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<uint> src, Span<RawBool> array) =>
            MemoryMarshal.Cast<uint, RawBool>(src).CopyTo(array);

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(Span<ulong> src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }
    }
}