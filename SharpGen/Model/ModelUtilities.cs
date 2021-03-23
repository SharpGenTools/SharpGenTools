#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpGen.Config;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Model
{
    public static class ModelUtilities
    {
        public static SyntaxTokenList VisibilityToTokenList(Visibility? visibility, params SyntaxKind[]? additionalKinds)
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
    }
}