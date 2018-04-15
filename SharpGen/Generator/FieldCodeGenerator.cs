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

        public FieldCodeGenerator(IDocumentationLinker documentation, ExternalDocCommentsReader docReader, bool explicitLayout)
            :base(documentation, docReader)
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
                                        GenerateIntToBoolConversion(IdentifierName(csElement.IntermediateMarshalName))))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(
                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(csElement.IntermediateMarshalName),
                                            CastExpression(
                                                ParseTypeName(csElement.MarshalType.QualifiedName),
                                                ParenthesizedExpression(
                                                    GenerateBoolToIntConversion(IdentifierName("value"))
                                        )))))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            })))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));
                yield return GenerateBackingField(csElement, csElement.MarshalType, explicitLayout ? csElement.Offset : (int?)null);
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
                                                ParseName(csElement.IntermediateMarshalName),
                                                ParenthesizedExpression(
                                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                        ParseName(csElement.IntermediateMarshalName),
                                                        ObjectCreationExpression(
                                                            ArrayType(ParseTypeName(csElement.PublicType.QualifiedName),
                                                            SingletonList(
                                                                ArrayRankSpecifier(
                                                                    SingletonSeparatedList<ExpressionSyntax>(
                                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.ArrayDimensionValue))))
                                                    ))))))))
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                    ParseName(csElement.IntermediateMarshalName),
                                                    IdentifierName("value"))))
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                        .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                                })))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));

                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : (int?)null, isArray: true);
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
                                        GenerateBitFieldGetter(csElement, GenerateIntToBoolConversion),
                                        GenerateBitFieldSetter(csElement, GenerateBoolToIntConversion)
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
                                        GenerateBitFieldGetter(csElement, valueExpression => CastExpression(ParseTypeName(csElement.PublicType.QualifiedName), valueExpression)),
                                        GenerateBitFieldSetter(csElement)
                                    }
                                    )))
                    .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                    .WithLeadingTrivia(Trivia(docComments));
                }
                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : (int?)null);
            }
            else
            {
                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : (int?)null, docTrivia: docComments, propertyBacking: false);
            }
        }

        private static BinaryExpressionSyntax GenerateIntToBoolConversion(ExpressionSyntax valueExpression)
        {
            return BinaryExpression(SyntaxKind.NotEqualsExpression,
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                valueExpression);
        }

        private static AccessorDeclarationSyntax GenerateBitFieldGetter(CsField csElement, Func<ExpressionSyntax, ExpressionSyntax> valueTransformation = null)
        {
            var valueExpression = BinaryExpression(SyntaxKind.BitwiseAndExpression,
                            ParenthesizedExpression(
                                BinaryExpression(SyntaxKind.RightShiftExpression,
                                    ParseName(csElement.IntermediateMarshalName),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitOffset)))),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitMask)));

            return AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithExpressionBody(ArrowExpressionClause(
                valueTransformation != null
                ? valueTransformation(ParenthesizedExpression(valueExpression))
                : valueExpression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static AccessorDeclarationSyntax GenerateBitFieldSetter(CsField csElement, Func<ExpressionSyntax, ExpressionSyntax> valueTransformation = null)
        {
            ExpressionSyntax valueExpression = IdentifierName("value");

            return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithExpressionBody(ArrowExpressionClause(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(csElement.IntermediateMarshalName)),
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
                                                IdentifierName(csElement.IntermediateMarshalName)),
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
                                                    valueTransformation != null ?
                                                    ParenthesizedExpression(valueTransformation(valueExpression))
                                                    : valueExpression,
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(csElement.BitMask)))),
                                            LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(csElement.BitOffset))))))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static ExpressionSyntax GenerateBoolToIntConversion(ExpressionSyntax valueExpression)
        {
            return ConditionalExpression(
                valueExpression,
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(1)),
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(0)));
        }

        private static MemberDeclarationSyntax GenerateBackingField(CsField field, CsTypeBase backingType, int? offset, bool isArray = false, bool propertyBacking = true, DocumentationCommentTriviaSyntax docTrivia = null)
        {
            var fieldDecl = FieldDeclaration(
               VariableDeclaration(isArray ?
                   ArrayType(ParseTypeName(backingType.QualifiedName), SingletonList(ArrayRankSpecifier()))
                   : ParseTypeName(backingType.QualifiedName),
                   SingletonSeparatedList(
                       VariableDeclarator(propertyBacking ? field.IntermediateMarshalName : field.Name)
                   )))
               .WithModifiers(propertyBacking ? TokenList(Token(SyntaxKind.InternalKeyword)) : TokenList(ParseTokens(field.VisibilityName)));

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
