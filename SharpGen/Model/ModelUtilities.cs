#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Config;
using SharpGen.CppModel;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Model;

public static class ModelUtilities
{
    private static readonly MemberAccessExpressionSyntax CallingConventionIdentifier = MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("System"),
                IdentifierName("Runtime")
            ),
            IdentifierName("InteropServices")
        ),
        IdentifierName("CallingConvention")
    );

    public static SyntaxTokenList VisibilityToTokenList(Visibility? visibility,
                                                        params SyntaxKind[]? additionalKinds)
    {
        var additionalKindsSequence = additionalKinds ?? Enumerable.Empty<SyntaxKind>();

        var kinds = visibility switch
        {
            { } visibilityValue => VisibilityToTokenKindList(visibilityValue),
            _ => Enumerable.Empty<SyntaxKind>()
        };

        return TokenList(kinds.Concat(additionalKindsSequence).Select(Token));
    }

    public static IReadOnlyCollection<SyntaxKind> VisibilityToTokenKindList(Visibility visibility)
    {
        List<SyntaxKind> list = new();

        if ((visibility & Visibility.Public) != 0)
        {
            list.Add(SyntaxKind.PublicKeyword);
        }
        else if ((visibility & Visibility.Protected) != 0)
        {
            list.Add(SyntaxKind.ProtectedKeyword);
        }
        else if ((visibility & Visibility.Internal) != 0)
        {
            list.Add(SyntaxKind.InternalKeyword);
        }
        else if ((visibility & Visibility.Private) != 0)
        {
            list.Add(SyntaxKind.PrivateKeyword);
        }
        else if ((visibility & Visibility.ProtectedInternal) != 0)
        {
            list.Add(SyntaxKind.ProtectedKeyword);
            list.Add(SyntaxKind.InternalKeyword);
        }
        else if ((visibility & Visibility.PrivateProtected) != 0)
        {
            list.Add(SyntaxKind.PrivateKeyword);
            list.Add(SyntaxKind.ProtectedKeyword);
        }

        if ((visibility & Visibility.Const) != 0)
            list.Add(SyntaxKind.ConstKeyword);

        if ((visibility & Visibility.Static) != 0)
            list.Add(SyntaxKind.StaticKeyword);

        if ((visibility & Visibility.Sealed) != 0)
            list.Add(SyntaxKind.SealedKeyword);

        if ((visibility & Visibility.Override) != 0)
            list.Add(SyntaxKind.OverrideKeyword);

        if ((visibility & Visibility.Abstract) != 0)
            list.Add(SyntaxKind.AbstractKeyword);

        if ((visibility & Visibility.Virtual) != 0)
            list.Add(SyntaxKind.VirtualKeyword);

        if ((visibility & Visibility.Readonly) != 0)
            list.Add(SyntaxKind.ReadOnlyKeyword);

        return list;
    }

    private static string ToManagedCallingConventionName(this CppCallingConvention callConv) =>
        callConv switch
        {
            CppCallingConvention.StdCall => nameof(CallingConvention.StdCall),
            CppCallingConvention.CDecl => nameof(CallingConvention.Cdecl),
            CppCallingConvention.ThisCall => nameof(CallingConvention.ThisCall),
            CppCallingConvention.FastCall => nameof(CallingConvention.FastCall),
            _ => nameof(CallingConvention.Winapi)
        };

    public static ExpressionSyntax GetManagedCallingConventionExpression(CppCallingConvention callingConvention) =>
        MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CallingConventionIdentifier,
            IdentifierName(callingConvention.ToManagedCallingConventionName())
        );

    public static string ToCallConvShortName(this CppCallingConvention callConv) => callConv switch
    {
        CppCallingConvention.StdCall => "Stdcall",
        CppCallingConvention.CDecl => "Cdecl",
        CppCallingConvention.ThisCall => "Thiscall",
        CppCallingConvention.FastCall => "Fastcall",
        _ => throw new ArgumentOutOfRangeException(nameof(callConv))
    };

    public static IEnumerable<CsBase> EnumerateDescendants(this CsBase element, bool withAdditionalItems = true)
    {
        yield return element;

        IEnumerable<CsBase> items = element.Items;
        if (withAdditionalItems)
            items = items.Concat(element.AdditionalItems);

        foreach (var descendant in items.SelectMany(x => EnumerateDescendants(x, withAdditionalItems)))
            yield return descendant;
    }

    public static IEnumerable<T> EnumerateDescendants<T>(this CsBase element, bool withAdditionalItems = true)
        where T : CsBase => element.EnumerateDescendants(withAdditionalItems).OfType<T>();
}