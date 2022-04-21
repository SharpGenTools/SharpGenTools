using System;

namespace SharpGen.Runtime;

public static partial class BooleanHelpers
{
    public static RawBool[] ConvertToRawBoolArray(ReadOnlySpan<bool> array)
    {
        var length = array.Length;
        var temp = new RawBool[length];
        for (var i = 0; i < length; i++)
            temp[i] = array[i];
        return temp;
    }

    public static bool[] ConvertToBoolArray(ReadOnlySpan<RawBool> array)
    {
        var length = array.Length;
        var temp = new bool[length];
        for (var i = 0; i < length; i++)
            temp[i] = array[i];
        return temp;
    }
}