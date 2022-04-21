using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator;

internal static class GuidConstantRoslynGenerator
{
    public static SyntaxToken[] GuidToTokens(Guid guid)
    {
        var extractedGuid = Unsafe.As<Guid, GuidExtractor>(ref guid);

        return new[]
        {
            Literal(extractedGuid.a),
            Literal(extractedGuid.b),
            Literal(extractedGuid.c),
            Literal(extractedGuid.d),
            Literal(extractedGuid.e),
            Literal(extractedGuid.f),
            Literal(extractedGuid.g),
            Literal(extractedGuid.h),
            Literal(extractedGuid.i),
            Literal(extractedGuid.j),
            Literal(extractedGuid.k)
        };
    }

    private static SyntaxToken Literal(int value) => SyntaxFactory.Literal("0x" + value.ToString("X8"), value);
    private static SyntaxToken Literal(byte value) => SyntaxFactory.Literal("0x" + value.ToString("X2"), value);
    private static SyntaxToken Literal(short value) => SyntaxFactory.Literal("0x" + value.ToString("X4"), value);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private readonly struct GuidExtractor
    {
#pragma warning disable 649
        public readonly int a;
        public readonly short b;
        public readonly short c;
        public readonly byte d;
        public readonly byte e;
        public readonly byte f;
        public readonly byte g;
        public readonly byte h;
        public readonly byte i;
        public readonly byte j;
        public readonly byte k;
#pragma warning restore 649
    }
}