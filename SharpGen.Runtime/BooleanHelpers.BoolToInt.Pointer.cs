using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

public static unsafe partial class BooleanHelpers
{
    /// <summary>
    /// Converts bool array to an array of integers.
    /// </summary>
    /// <param name="array">The bool array.</param>
    /// <param name="dest">The destination array of integers.</param>
    public static void ConvertToIntArray(Span<bool> array, byte* dest)
    {
        fixed (void* src = array)
            Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
    }

    /// <summary>
    /// Converts bool array to an array of integers.
    /// </summary>
    /// <param name="array">The bool array.</param>
    /// <param name="dest">The destination array of integers.</param>
    public static void ConvertToIntArray(Span<bool> array, short* dest)
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
    public static void ConvertToIntArray(Span<bool> array, int* dest)
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
    public static void ConvertToIntArray(Span<bool> array, long* dest)
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
    public static void ConvertToIntArray(Span<bool> array, sbyte* dest)
    {
        fixed (void* src = array)
            Unsafe.CopyBlockUnaligned(dest, src, (uint) array.Length);
    }

    /// <summary>
    /// Converts bool array to an array of integers.
    /// </summary>
    /// <param name="array">The bool array.</param>
    /// <param name="dest">The destination array of integers.</param>
    public static void ConvertToIntArray(Span<bool> array, ushort* dest)
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
    public static void ConvertToIntArray(Span<bool> array, uint* dest)
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
    public static void ConvertToIntArray(Span<bool> array, ulong* dest)
    {
        var arrayLength = array.Length;
        for (var i = 0; i < arrayLength; i++)
            dest[i] = (ulong) (array[i] ? 1 : 0);
    }
}