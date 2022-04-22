using System;
using System.Collections.Generic;
using SharpGen.Model;

namespace SharpGen.Transform;

public partial class TypeRegistry
{
    public static readonly CsFundamentalType Void = new(
        typeof(void), new PrimitiveTypeIdentity(PrimitiveTypeCode.Void), "void"
    );

    public static readonly CsFundamentalType VoidPtr = new(
        typeof(void*), new PrimitiveTypeIdentity(PrimitiveTypeCode.Void, 1), "void*"
    );

    public static readonly CsFundamentalType Int32 = new(
        typeof(int), new PrimitiveTypeIdentity(PrimitiveTypeCode.Int32), "int"
    );

    public static readonly CsFundamentalType Int16 = new(
        typeof(short), new PrimitiveTypeIdentity(PrimitiveTypeCode.Int16), "short"
    );

    public static readonly CsFundamentalType Float = new(
        typeof(float), new PrimitiveTypeIdentity(PrimitiveTypeCode.Float), "float"
    );

    public static readonly CsFundamentalType Double = new(
        typeof(double), new PrimitiveTypeIdentity(PrimitiveTypeCode.Double), "double"
    );

    public static readonly CsFundamentalType Int64 = new(
        typeof(long), new PrimitiveTypeIdentity(PrimitiveTypeCode.Int64), "long"
    );

    public static readonly CsFundamentalType UInt32 = new(
        typeof(uint), new PrimitiveTypeIdentity(PrimitiveTypeCode.UInt32), "uint"
    );

    public static readonly CsFundamentalType UInt64 = new(
        typeof(ulong), new PrimitiveTypeIdentity(PrimitiveTypeCode.UInt64), "ulong"
    );

    public static readonly CsFundamentalType UInt16 = new(
        typeof(ushort), new PrimitiveTypeIdentity(PrimitiveTypeCode.UInt16), "ushort"
    );

    public static readonly CsFundamentalType UInt8 = new(
        typeof(byte), new PrimitiveTypeIdentity(PrimitiveTypeCode.UInt8), "byte"
    );

    public static readonly CsFundamentalType Int8 = new(
        typeof(sbyte), new PrimitiveTypeIdentity(PrimitiveTypeCode.Int8), "sbyte"
    );

    public static readonly CsFundamentalType Boolean = new(
        typeof(bool), new PrimitiveTypeIdentity(PrimitiveTypeCode.Boolean), "bool"
    );

    public static readonly CsFundamentalType Char = new(
        typeof(char), new PrimitiveTypeIdentity(PrimitiveTypeCode.Char), "char"
    );

    public static readonly CsFundamentalType Decimal = new(
        typeof(decimal), new PrimitiveTypeIdentity(PrimitiveTypeCode.Decimal), "decimal"
    );

    public static readonly CsFundamentalType String = new(
        typeof(string), new PrimitiveTypeIdentity(PrimitiveTypeCode.String), "string"
    );

    public static readonly CsFundamentalType IntPtr = new(
        typeof(IntPtr), new PrimitiveTypeIdentity(PrimitiveTypeCode.IntPtr), "System.IntPtr"
    );

    public static readonly CsFundamentalType UIntPtr = new(
        typeof(UIntPtr), new PrimitiveTypeIdentity(PrimitiveTypeCode.UIntPtr), "System.UIntPtr"
    );

    private static readonly Dictionary<PrimitiveTypeIdentity, CsFundamentalType> PrimitiveTypeEntriesByIdentity =
        new()
        {
            // ReSharper disable PossibleInvalidOperationException
            [Void.PrimitiveTypeIdentity.Value] = Void,
            [VoidPtr.PrimitiveTypeIdentity.Value] = VoidPtr,
            [Int32.PrimitiveTypeIdentity.Value] = Int32,
            [Int16.PrimitiveTypeIdentity.Value] = Int16,
            [Float.PrimitiveTypeIdentity.Value] = Float,
            [Double.PrimitiveTypeIdentity.Value] = Double,
            [Int64.PrimitiveTypeIdentity.Value] = Int64,
            [UInt32.PrimitiveTypeIdentity.Value] = UInt32,
            [UInt64.PrimitiveTypeIdentity.Value] = UInt64,
            [UInt16.PrimitiveTypeIdentity.Value] = UInt16,
            [UInt8.PrimitiveTypeIdentity.Value] = UInt8,
            [Int8.PrimitiveTypeIdentity.Value] = Int8,
            [Boolean.PrimitiveTypeIdentity.Value] = Boolean,
            [Char.PrimitiveTypeIdentity.Value] = Char,
            [Decimal.PrimitiveTypeIdentity.Value] = Decimal,
            [String.PrimitiveTypeIdentity.Value] = String,
            [IntPtr.PrimitiveTypeIdentity.Value] = IntPtr,
            [UIntPtr.PrimitiveTypeIdentity.Value] = UIntPtr,
            // ReSharper restore PossibleInvalidOperationException
        };

    private static readonly Dictionary<string, CsFundamentalType> PrimitiveTypeEntriesByName = new()
    {
        ["void"] = Void,
        ["void*"] = VoidPtr,
        ["int"] = Int32,
        ["short"] = Int16,
        ["float"] = Float,
        ["double"] = Double,
        ["long"] = Int64,
        ["uint"] = UInt32,
        ["ulong"] = UInt64,
        ["ushort"] = UInt16,
        ["byte"] = UInt8,
        ["sbyte"] = Int8,
        ["bool"] = Boolean,
        ["char"] = Char,
        ["decimal"] = Decimal,
        ["string"] = String,
        ["IntPtr"] = IntPtr,
        ["UIntPtr"] = UIntPtr,
        ["System.IntPtr"] = IntPtr,
        ["System.UIntPtr"] = UIntPtr,
        ["nint"] = IntPtr,
        ["nuint"] = UIntPtr,
    };

    private static readonly Dictionary<PrimitiveTypeCode, Type> PrimitiveRuntimeTypesByCode = new()
    {
        [PrimitiveTypeCode.Void] = typeof(void),
        [PrimitiveTypeCode.Int32] = typeof(int),
        [PrimitiveTypeCode.Int16] = typeof(short),
        [PrimitiveTypeCode.Float] = typeof(float),
        [PrimitiveTypeCode.Double] = typeof(double),
        [PrimitiveTypeCode.Int64] = typeof(long),
        [PrimitiveTypeCode.UInt32] = typeof(uint),
        [PrimitiveTypeCode.UInt64] = typeof(ulong),
        [PrimitiveTypeCode.UInt16] = typeof(ushort),
        [PrimitiveTypeCode.UInt8] = typeof(byte),
        [PrimitiveTypeCode.Int8] = typeof(sbyte),
        [PrimitiveTypeCode.Boolean] = typeof(bool),
        [PrimitiveTypeCode.Char] = typeof(char),
        [PrimitiveTypeCode.Decimal] = typeof(decimal),
        [PrimitiveTypeCode.String] = typeof(string),
        [PrimitiveTypeCode.IntPtr] = typeof(IntPtr),
        [PrimitiveTypeCode.UIntPtr] = typeof(UIntPtr),
    };
}