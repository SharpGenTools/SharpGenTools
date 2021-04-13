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
    internal sealed class FieldCodeGenerator : MemberCodeGeneratorBase<CsField>
    {
        private readonly bool explicitLayout;

        public FieldCodeGenerator(IDocumentationLinker documentation, ExternalDocCommentsReader docReader, bool explicitLayout)
            :base(documentation, docReader)
        {
            this.explicitLayout = explicitLayout;
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsField csElement)
        {
            var docComments = Trivia(GenerateDocumentationTrivia(csElement));
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
                    .WithModifiers(csElement.VisibilityTokenList)
                    .WithLeadingTrivia(docComments);
                yield return GenerateBackingField(csElement, csElement.MarshalType, explicitLayout ? csElement.Offset : null);
            }
            else if (csElement.IsArray && !csElement.IsString)
            {
                var elementType = ParseTypeName(csElement.PublicType.QualifiedName);
                yield return PropertyDeclaration(ArrayType(elementType, SingletonList(ArrayRankSpecifier())), csElement.Name)
                    .WithAccessorList(
                        AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                            AssignmentExpression(SyntaxKind.CoalesceAssignmentExpression,
                                                ParseName(csElement.IntermediateMarshalName),
                                                ObjectCreationExpression(
                                                    ArrayType(elementType,
                                                    SingletonList(
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.ArrayDimensionValue))))
                                            ))))))
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                    ParseName(csElement.IntermediateMarshalName),
                                                    IdentifierName("value"))))
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                        .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                                })))
                    .WithModifiers(csElement.VisibilityTokenList)
                    .WithLeadingTrivia(docComments);

                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : null, isArray: true);
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
                    .WithModifiers(csElement.VisibilityTokenList)
                    .WithLeadingTrivia(docComments);
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
                    .WithModifiers(csElement.VisibilityTokenList)
                    .WithLeadingTrivia(docComments);
                }
                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : null);
            }
            else
            {
                yield return GenerateBackingField(csElement, csElement.PublicType, explicitLayout ? csElement.Offset : null, docTrivia: docComments, propertyBacking: false);
            }
        }

        private static BinaryExpressionSyntax GenerateIntToBoolConversion(ExpressionSyntax valueExpression) =>
            BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                valueExpression
            );

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

        private static ExpressionSyntax GenerateBoolToIntConversion(ExpressionSyntax valueExpression) =>
            ConditionalExpression(
                valueExpression,
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
            );

        private static MemberDeclarationSyntax GenerateBackingField(CsField field, CsTypeBase backingType, int? offset, bool isArray = false, bool propertyBacking = true, SyntaxTrivia? docTrivia = null)
        {
            var elementType = ParseTypeName(backingType.QualifiedName);

            var fieldDecl = FieldDeclaration(
                    VariableDeclaration(
                        isArray
                            ? ArrayType(elementType, SingletonList(ArrayRankSpecifier()))
                            : elementType,
                        SingletonSeparatedList(
                            VariableDeclarator(propertyBacking ? field.IntermediateMarshalName : field.Name)
                        )
                    )
                )
               .WithModifiers(
                    propertyBacking
                        ? TokenList(Token(SyntaxKind.InternalKeyword))
                        : field.VisibilityTokenList
                );

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

            return docTrivia is { } trivia ? fieldDecl.WithLeadingTrivia(trivia) : fieldDecl;
        }
    }
}
