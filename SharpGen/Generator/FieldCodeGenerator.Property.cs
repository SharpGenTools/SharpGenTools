using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed partial class FieldCodeGenerator
    {
        private PropertyDeclarationSyntax GenerateProperty(CsField csElement, TypeSyntax propertyType,
                                                           PropertyValueGetTransform getterTransform,
                                                           PropertyValueSetTransform setterTransform) =>
            AddDocumentationTrivia(
                PropertyDeclaration(propertyType, csElement.Name)
                   .WithAccessorList(
                        AccessorList(
                            List(
                                new[]
                                {
                                    GenerateGetter(csElement, getterTransform),
                                    GenerateSetter(csElement, setterTransform)
                                }
                            )
                        )
                    )
                   .WithModifiers(csElement.VisibilityTokenList),
                csElement
            );

        private delegate ExpressionSyntax PropertyValueGetTransform(ExpressionSyntax value);

        private delegate ExpressionSyntax PropertyValueSetTransform(ExpressionSyntax oldValue, ExpressionSyntax value);

        private static PropertyValueGetTransform Compose(PropertyValueGetTransform second,
                                                         PropertyValueGetTransform first)
        {
            if (second == null)
                return first;
            if (first == null)
                return second;

            return x =>
            {
                var value = first(x);
                return second(GeneratorHelpers.WrapInParentheses(value));
            };
        }

        private static PropertyValueSetTransform Compose(PropertyValueSetTransform second,
                                                         PropertyValueSetTransform first)
        {
            if (second == null)
                return first;
            if (first == null)
                return second;

            return (oldValue, x) =>
            {
                var value = first(oldValue, x);
                return second(oldValue, GeneratorHelpers.WrapInParentheses(value));
            };
        }

        private static AccessorDeclarationSyntax GenerateGetter(CsField csElement,
                                                                PropertyValueGetTransform valueTransformation)
        {
            ExpressionSyntax valueExpression = ParseName(csElement.IntermediateMarshalName);

            return AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                  .WithExpressionBody(
                       ArrowExpressionClause(
                           valueTransformation != null
                               ? valueTransformation(GeneratorHelpers.WrapInParentheses(valueExpression))
                               : valueExpression
                       )
                   )
                  .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static AccessorDeclarationSyntax GenerateSetter(CsField csElement,
                                                                PropertyValueSetTransform valueTransformation)
        {
            ExpressionSyntax valueExpression = IdentifierName("value");

            var storage = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(csElement.IntermediateMarshalName)
            );

            return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                  .WithExpressionBody(
                       ArrowExpressionClause(
                           AssignmentExpression(
                               SyntaxKind.SimpleAssignmentExpression,
                               storage,
                               valueTransformation != null
                                   ? valueTransformation(storage, valueExpression)
                                   : valueExpression
                           )
                       )
                   )
                  .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }
    }
}