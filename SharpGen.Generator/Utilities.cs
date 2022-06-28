using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

public static class Utilities
{
    private static AttributeData? TypeToAttribute(INamespaceOrTypeSymbol? typeSymbol, string name)
    {
        if (typeSymbol is null)
            return null;

        var attributes = typeSymbol.GetAttributes()
                                   .Where(x => x.AttributeClass?.ToDisplayString() == name)
                                   .ToArray();
        return attributes.Length == 0 ? null : attributes.ExclusiveOrDefault();
    }

    public static ITypeSymbol? GetVtblAttribute(this INamespaceOrTypeSymbol? typeSymbol)
    {
        if (TypeToAttribute(typeSymbol, "SharpGen.Runtime.VtblAttribute") is not { ConstructorArguments: var args })
            return null;

        if (args.Single() is
            {
                Kind: TypedConstantKind.Type, Value: ITypeSymbol
                {
                    IsValueType: false, IsReferenceType: true, IsStatic: true,
                    IsTupleType: false, IsNativeIntegerType: false, IsAnonymousType: false
                } vtblType
            }
           )
            return vtblType;

        throw new Exception();
    }

    public static Guid? GetGuidAttribute(this INamespaceOrTypeSymbol? typeSymbol)
    {
        if (TypeToAttribute(typeSymbol, "System.Runtime.InteropServices.GuidAttribute") is not
            { ConstructorArguments: var args })
            return null;

        if (args.Single() is
            {
                Kind: TypedConstantKind.Primitive, Value: string { Length: > 0 } guidString
            }
           )
            return Guid.Parse(guidString);

        throw new Exception();
    }

    public static TSource? ExclusiveOrDefault<TSource>(this IEnumerable<TSource>? source) where TSource : class
    {
        if (source == null) return default;

        if (source is IList<TSource> list)
        {
            if (list.Count == 1)
                return list[0];
        }
        else
        {
            using var e = source.GetEnumerator();
            if (!e.MoveNext())
                return default;

            var result = e.Current;
            if (!e.MoveNext())
                return result;
        }

        return default;
    }

    public static TSource? ExclusiveOrDefault<TSource>(this IEnumerable<TSource>? source, Func<TSource, bool> predicate)
        where TSource : class
    {
        if (source == null) return default;
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        using var e = source.GetEnumerator();
        while (e.MoveNext())
        {
            var result = e.Current;
            if (predicate(result))
            {
                while (e.MoveNext())
                    if (predicate(e.Current))
                        return default;

                return result;
            }
        }

        return default;
    }

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

    public static ArgumentListSyntax GetGuidParameters(Guid parsedGuid)
    {
        var extractedGuid = Unsafe.As<Guid, GuidExtractor>(ref parsedGuid);

        return ArgumentList(
            SeparatedList(
                new[]
                {
                    Literal4(extractedGuid.a), Literal2(extractedGuid.b), Literal2(extractedGuid.c),
                    Literal1(extractedGuid.d), Literal1(extractedGuid.e), Literal1(extractedGuid.f),
                    Literal1(extractedGuid.g), Literal1(extractedGuid.h), Literal1(extractedGuid.i),
                    Literal1(extractedGuid.j), Literal1(extractedGuid.k)
                }
            )
        );

        static ArgumentSyntax LiteralArgument(SyntaxToken x) =>
            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, x));

        static ArgumentSyntax Literal1(byte value) =>
            LiteralArgument(Literal("0x" + value.ToString("X2"), value));

        static ArgumentSyntax Literal2(short value) =>
            LiteralArgument(Literal("0x" + value.ToString("X4"), value));

        static ArgumentSyntax Literal4(int value) =>
            LiteralArgument(Literal("0x" + value.ToString("X8"), value));
    }

    /// <summary>
    /// Returns true if the given symbol contains a base class with a given type name.
    /// </summary>
    /// <param name="symbol">The class symbol.</param>
    /// <param name="fullTypeName">Full name of the type, e.g. \"SharpGen.Runtime.CallbackBase\"</param>
    /// <returns></returns>
    public static bool HasBaseClass(this ITypeSymbol symbol, string fullTypeName)
    {
        var baseClass = symbol.BaseType;

        while (baseClass != null)
        {
            if (baseClass.ToDisplayString() == fullTypeName)
                return true;

            baseClass = baseClass.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if an enum is any of the following given param values.
    /// </summary>
    public static bool IsAnyOfFollowing<T>(T value, params T[] values) where T : Enum
    {
        foreach (var val in values)
        {
            if (value.Equals(val))
                return true;
        }

        return false;
    }
}