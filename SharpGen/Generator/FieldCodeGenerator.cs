using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed partial class FieldCodeGenerator : MemberCodeGeneratorBase<CsField>
    {
        private readonly bool explicitLayout;

        public FieldCodeGenerator(Ioc ioc, bool explicitLayout) : base(ioc)
        {
            this.explicitLayout = explicitLayout;
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsField csElement)
        {
            if (csElement.IsBoolToInt && !csElement.IsArray)
            {
                yield return GenerateBackingField(csElement, csElement.MarshalType);

                yield return GenerateProperty(
                    csElement, PredefinedType(Token(SyntaxKind.BoolKeyword)),
                    GeneratorHelpers.GenerateIntToBoolConversion,
                    (_, value) => GeneratorHelpers.CastExpression(
                        ParseTypeName(csElement.MarshalType.QualifiedName),
                        GeneratorHelpers.GenerateBoolToIntConversion(value)
                    )
                );
            }
            else if (csElement.IsArray && !csElement.IsString)
            {
                var elementType = ParseTypeName(csElement.PublicType.QualifiedName);

                yield return GenerateBackingField(csElement, csElement.PublicType, isArray: true);

                yield return GenerateProperty(
                    csElement, ArrayType(elementType, SingletonList(ArrayRankSpecifier())),
                    value => AssignmentExpression(
                        SyntaxKind.CoalesceAssignmentExpression,
                        value,
                        ObjectCreationExpression(
                            ArrayType(
                                elementType,
                                SingletonList(
                                    ArrayRankSpecifier(
                                        SingletonSeparatedList<ExpressionSyntax>(
                                            LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(csElement.ArrayDimensionValue)
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    ),
                    null
                );
            }
            else if (csElement.IsBitField)
            {
                PropertyValueGetTransform getterTransform;
                PropertyValueSetTransform setterTransform;
                TypeSyntax propertyType;

                if (csElement.IsBoolBitField)
                {
                    getterTransform = GeneratorHelpers.GenerateIntToBoolConversion;
                    setterTransform = (_, value) => GeneratorHelpers.GenerateBoolToIntConversion(value);
                    propertyType = PredefinedType(Token(SyntaxKind.BoolKeyword));
                }
                else
                {
                    getterTransform = valueExpression => GeneratorHelpers.CastExpression(
                        ParseTypeName(csElement.PublicType.QualifiedName),
                        valueExpression
                    );
                    setterTransform = null;
                    propertyType = ParseTypeName(csElement.PublicType.QualifiedName);
                }

                yield return GenerateBackingField(csElement, csElement.PublicType);

                var bitMask = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.BitMask));
                var bitOffset = LiteralExpression(
                    SyntaxKind.NumericLiteralExpression, Literal(csElement.BitOffset)
                );

                yield return GenerateProperty(
                    csElement, propertyType,
                    Compose(
                        getterTransform,
                        value => BinaryExpression(
                            SyntaxKind.BitwiseAndExpression,
                            GeneratorHelpers.WrapInParentheses(
                                BinaryExpression(SyntaxKind.RightShiftExpression, value, bitOffset)
                            ),
                            bitMask
                        )
                    ),
                    Compose(
                        (oldValue, value) => GeneratorHelpers.CastExpression(
                            ParseTypeName(csElement.PublicType.QualifiedName),
                            BinaryExpression(
                                SyntaxKind.BitwiseOrExpression,
                                GeneratorHelpers.WrapInParentheses(
                                    BinaryExpression(
                                        SyntaxKind.BitwiseAndExpression,
                                        oldValue,
                                        PrefixUnaryExpression(
                                            SyntaxKind.BitwiseNotExpression,
                                            GeneratorHelpers.WrapInParentheses(
                                                BinaryExpression(SyntaxKind.LeftShiftExpression, bitMask, bitOffset)
                                            )
                                        )
                                    )
                                ),
                                GeneratorHelpers.WrapInParentheses(
                                    BinaryExpression(
                                        SyntaxKind.LeftShiftExpression,
                                        GeneratorHelpers.WrapInParentheses(
                                            BinaryExpression(SyntaxKind.BitwiseAndExpression, value, bitMask)
                                        ),
                                        bitOffset
                                    )
                                )
                            )
                        ),
                        setterTransform
                    )
                );
            }
            else
            {
                yield return GenerateBackingField(
                    csElement, csElement.PublicType, propertyBacking: false, document: true
                );
            }
        }

        private MemberDeclarationSyntax GenerateBackingField(CsField field, CsTypeBase backingType,
                                                             bool isArray = false, bool propertyBacking = true,
                                                             bool document = false)
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

            if (explicitLayout)
                fieldDecl = AddFieldOffsetAttribute(fieldDecl, field.Offset);

            return document ? AddDocumentationTrivia(fieldDecl, field) : fieldDecl;
        }
    }
}
