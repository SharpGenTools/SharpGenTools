using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime
{
    public static unsafe partial class BooleanHelpers
    {
        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(byte* src, Span<RawBool> array)
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
        public static void ConvertToBoolArray(short* src, Span<RawBool> array)
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
        public static void ConvertToBoolArray(int* src, Span<RawBool> array)
        {
            fixed (void* dest = array)
                Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(long* src, Span<RawBool> array)
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
        public static void ConvertToBoolArray(sbyte* src, Span<RawBool> array)
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
        public static void ConvertToBoolArray(ushort* src, Span<RawBool> array)
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
        public static void ConvertToBoolArray(uint* src, Span<RawBool> array)
        {
            fixed (void* dest = array)
                Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
        }

        /// <summary>
        /// Converts integer array to bool array.
        /// </summary>
        /// <param name="src">A pointer to the array of integers.</param>
        /// <param name="array">The target bool array to fill.</param>
        public static void ConvertToBoolArray(ulong* src, Span<RawBool> array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
                array[i] = src[i] != 0;
        }
    }
}