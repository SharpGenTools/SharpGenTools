using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class FieldCodeGenerator : MemberCodeGeneratorBase<CsField>
    {
        private readonly bool explicitLayout;

        public FieldCodeGenerator(IDocumentationLinker documentation, bool explicitLayout)
            :base(documentation)
        {
            this.explicitLayout = explicitLayout;
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsField csElement)
        {
            var docComments = GenerateDocumentationTrivia(csElement);
            if (csElement.IsBoolToInt && !csElement.IsArray)
            {
                yield return PropertyDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), csElement.Name)
                    .WithAccessorList(
                    AccessorList(
                        List(
                            new[]
                            {
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(
                                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                ParseName($"_{csElement.Name}"),
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(CastExpression(ParseTypeName(csElement.PublicType.QualifiedName), ParseName("value"))))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            })))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));
                yield return GenerateBackingField(csElement.Name, csElement.MarshalType, csElement.VisibilityName, explicitLayout ? csElement.Offset : (int?)null);
            }
            else if (csElement.IsArray && csElement.PublicType.QualifiedName != "System.String")
            {
                yield return PropertyDeclaration(ArrayType(ParseTypeName(csElement.PublicType.QualifiedName), SingletonList(ArrayRankSpecifier())), csElement.Name)
                    .WithAccessorList(
                        AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                            BinaryExpression(SyntaxKind.CoalesceExpression,
                                                ParseName($"_{csElement.Name}"),
                                                ParenthesizedExpression(
                                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                        ParseName($"_{csElement.Name}"),
                                                        ObjectCreationExpression(
                                                            ArrayType(ParseTypeName(csElement.PublicType.QualifiedName),
                                                            SingletonList(
                                                                ArrayRankSpecifier(
                                                                    SingletonSeparatedList<ExpressionSyntax>(
                                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.ArrayDimensionValue))))
                                                    ))))))))
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                })))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));

                yield return GenerateBackingField(csElement.Name, csElement.PublicType, csElement.VisibilityName, explicitLayout ? csElement.Offset : (int?)null, isArray: true);
            }
            else if (csElement.IsBitField)
            {
                if (csElement.BitMask == 1)
                {
                    yield return PropertyDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), csElement.Name)
                        .WithAccessorList(
                            AccessorList(
                                List(
                                    new[]
                                    {
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithExpressionBody(ArrowExpressionClause(
                                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                                                    ParenthesizedExpression(
                                                        BinaryExpression(SyntaxKind.BitwiseAndExpression,
                                                            ParenthesizedExpression(
                                                                BinaryExpression(SyntaxKind.RightShiftExpression,
                                                                    ParseName($"_{csElement.Name}"),
                                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitOffset)))),
                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitMask)))))))
                                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithExpressionBody(ArrowExpressionClause(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName($"_{csElement.Name}")),
                                                    CastExpression(
                                                        ParseTypeName(csElement.PublicType.QualifiedName),
                                                        ParenthesizedExpression(
                                                            BinaryExpression(
                                                                SyntaxKind.BitwiseOrExpression,
                                                                ParenthesizedExpression(
                                                                    BinaryExpression(
                                                                        SyntaxKind.BitwiseAndExpression,
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            ThisExpression(),
                                                                            IdentifierName(($"_{csElement.Name}"))),
                                                                        PrefixUnaryExpression(
                                                                            SyntaxKind.BitwiseNotExpression,
                                                                            ParenthesizedExpression(
                                                                                BinaryExpression(
                                                                                    SyntaxKind.LeftShiftExpression,
                                                                                    LiteralExpression(
                                                                                        SyntaxKind.NumericLiteralExpression,
                                                                                        Literal(csElement.BitMask)),
                                                                                    LiteralExpression(
                                                                                        SyntaxKind.NumericLiteralExpression,
                                                                                        Literal(csElement.BitOffset))))))),
                                                                ParenthesizedExpression(
                                                                    BinaryExpression(
                                                                        SyntaxKind.LeftShiftExpression,
                                                                        ParenthesizedExpression(
                                                                            BinaryExpression(
                                                                                SyntaxKind.BitwiseAndExpression,
                                                                                ParenthesizedExpression(
                                                                                    ConditionalExpression(
                                                                                        IdentifierName("value"),
                                                                                        LiteralExpression(
                                                                                            SyntaxKind.NumericLiteralExpression,
                                                                                            Literal(1)),
                                                                                        LiteralExpression(
                                                                                            SyntaxKind.NumericLiteralExpression,
                                                                                            Literal(0)))),
                                                                                LiteralExpression(
                                                                                    SyntaxKind.NumericLiteralExpression,
                                                                                    Literal(csElement.BitMask)))),
                                                                        LiteralExpression(
                                                                            SyntaxKind.NumericLiteralExpression,
                                                                            Literal(csElement.BitOffset))))))))))
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                    }
                                    )))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));
                }
                else
                {

                    yield return PropertyDeclaration(ParseTypeName(csElement.PublicType.QualifiedName), csElement.Name)
                        .WithAccessorList(
                            AccessorList(
                                List(
                                    new[]
                                    {
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithExpressionBody(ArrowExpressionClause(
                                                CastExpression(ParseTypeName(csElement.PublicType.QualifiedName),
                                                    ParenthesizedExpression(
                                                        BinaryExpression(SyntaxKind.BitwiseAndExpression,
                                                            ParenthesizedExpression(
                                                                BinaryExpression(SyntaxKind.RightShiftExpression,
                                                                    ParseName($"_{csElement.Name}"),
                                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitOffset)))),
                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitMask)))))))
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithExpressionBody(ArrowExpressionClause(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName($"_{csElement.Name}")),
                                                    CastExpression(
                                                        ParseTypeName(csElement.PublicType.QualifiedName),
                                                        ParenthesizedExpression(
                                                            BinaryExpression(
                                                                SyntaxKind.BitwiseOrExpression,
                                                                ParenthesizedExpression(
                                                                    BinaryExpression(
                                                                        SyntaxKind.BitwiseAndExpression,
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            ThisExpression(),
                                                                            IdentifierName(($"_{csElement.Name}"))),
                                                                        PrefixUnaryExpression(
                                                                            SyntaxKind.BitwiseNotExpression,
                                                                            ParenthesizedExpression(
                                                                                BinaryExpression(
                                                                                    SyntaxKind.LeftShiftExpression,
                                                                                    LiteralExpression(
                                                                                        SyntaxKind.NumericLiteralExpression,
                                                                                        Literal(csElement.BitMask)),
                                                                                    LiteralExpression(
                                                                                        SyntaxKind.NumericLiteralExpression,
                                                                                        Literal(csElement.BitOffset))))))),
                                                                ParenthesizedExpression(
                                                                    BinaryExpression(
                                                                        SyntaxKind.LeftShiftExpression,
                                                                        ParenthesizedExpression(
                                                                            BinaryExpression(
                                                                                SyntaxKind.BitwiseAndExpression,
                                                                                IdentifierName("value"),
                                                                                LiteralExpression(
                                                                                    SyntaxKind.NumericLiteralExpression,
                                                                                    Literal(csElement.BitMask)))),
                                                                        LiteralExpression(
                                                                            SyntaxKind.NumericLiteralExpression,
                                                                            Literal(csElement.BitOffset))))))))))
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                    }
                                    )))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));
                }
                yield return GenerateBackingField(csElement.Name, csElement.PublicType, csElement.VisibilityName, explicitLayout ? csElement.Offset : (int?)null);
            }
            else
            {
                yield return GenerateBackingField(csElement.Name, csElement.PublicType, csElement.VisibilityName, explicitLayout ? csElement.Offset : (int?)null, docTrivia: docComments, propertyBacking: false);
            }
        }

        private static MemberDeclarationSyntax GenerateBackingField(string name, CsTypeBase type, string visibility, int? offset, bool isArray = false, bool propertyBacking = true, DocumentationCommentTriviaSyntax docTrivia = null)
        {
            var fieldDecl = FieldDeclaration(
               VariableDeclaration(isArray ?
                   ArrayType(ParseTypeName(type.QualifiedName), SingletonList(ArrayRankSpecifier()))
                   : ParseTypeName(type.QualifiedName),
                   SingletonSeparatedList(
                       VariableDeclarator(propertyBacking ? $"_{name}" : name)
                   )))
               .WithModifiers(propertyBacking ? TokenList(Token(SyntaxKind.InternalKeyword)) : TokenList(ParseTokens(visibility)));

            if (offset.HasValue)
            {
                fieldDecl = fieldDecl.WithAttributeLists(SingletonList(
                    AttributeList(
                        SingletonSeparatedList(Attribute(
                            ParseName("System.Runtime.InteropServices.FieldOffset"),
                            AttributeArgumentList(
                                SingletonSeparatedList(AttributeArgument(
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(offset.Value))))))))
                ));
            }
            return docTrivia != null ? fieldDecl.WithLeadingTrivia(Trivia(docTrivia)) : fieldDecl;
        }

    }
}
