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
        public FieldCodeGenerator(IDocumentationLinker documentation, ExternalDocCommentsReader docReader)
            :base(documentation, docReader)
        {
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
                yield return GenerateBackingField(csElement, csElement.MarshalType);
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

                yield return GenerateBackingField(csElement, csElement.PublicType, true);
            }
            else if (csElement.IsBitField)
            {
                if (csElement.IsBoolBitField)
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
                yield return GenerateBackingField(csElement, csElement.PublicType);
            }
            else
            {
                yield return GenerateBackingField(csElement, csElement.PublicType, docTrivia: docComments, propertyBacking: false);
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
            var valueExpression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(csElement.IntermediateMarshalName)
            );

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
                  .WithExpressionBody(
                       ArrowExpressionClause(
                           AssignmentExpression(
                               SyntaxKind.SimpleAssignmentExpression,
                               MemberAccessExpression(
                                   SyntaxKind.SimpleMemberAccessExpression,
                                   ThisExpression(),
                                   IdentifierName(csElement.IntermediateMarshalName)),
                               CastExpression(
                                   ParseTypeName(csElement.PublicType.QualifiedName),
                                   ParenthesizedExpression(
                                       valueTransformation != null
                                           ? ParenthesizedExpression(valueTransformation(valueExpression))
                                           : valueExpression)
                               ))))
                  .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static ExpressionSyntax GenerateBoolToIntConversion(ExpressionSyntax valueExpression) =>
            ConditionalExpression(
                valueExpression,
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
            );

        private static MemberDeclarationSyntax GenerateBackingField(CsField field, CsTypeBase backingType,
                                                                    bool isArray = false, bool propertyBacking = true,
                                                                    SyntaxTrivia? docTrivia = null)
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

            return docTrivia is { } trivia ? fieldDecl.WithLeadingTrivia(trivia) : fieldDecl;
        }
    }
}
