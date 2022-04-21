using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

public static partial class BooleanHelpers
{
    /// <summary>
    /// Converts integer array to bool array.
    /// </summary>
    /// <param name="src">A pointer to the array of integers.</param>
    /// <param name="array">The target bool array to fill.</param>
    public static void ConvertToBoolArray(Span<byte> src, Span<bool> array) =>
        src.CopyTo(MemoryMarshal.AsBytes(array));

    /// <summary>
    /// Converts integer array to bool array.
    /// </summary>
    /// <param name="src">A pointer to the array of integers.</param>
    /// <param name="array">The target bool array to fill.</param>
    public static void ConvertToBoolArray(Span<short> src, Span<bool> array)
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
    public static void ConvertToBoolArray(Span<int> src, Span<bool> array)
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
    public static void ConvertToBoolArray(Span<long> src, Span<bool> array)
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
    public static void ConvertToBoolArray(Span<sbyte> src, Span<bool> array) =>
        MemoryMarshal.AsBytes(src).CopyTo(MemoryMarshal.AsBytes(array));

    /// <summary>
    /// Converts integer array to bool array.
    /// </summary>
    /// <param name="src">A pointer to the array of integers.</param>
    /// <param name="array">The target bool array to fill.</param>
    public static void ConvertToBoolArray(Span<ushort> src, Span<bool> array)
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
    public static void ConvertToBoolArray(Span<uint> src, Span<bool> array)
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
    public static void ConvertToBoolArray(Span<ulong> src, Span<bool> array)
    {
        var arrayLength = array.Length;
        for (var i = 0; i < arrayLength; i++)
            array[i] = src[i] != 0;
    }
}