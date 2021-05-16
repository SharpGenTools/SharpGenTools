using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed partial class ValueTypeArrayMarshaller
    {
        private enum ArrayCopyDirection : byte
        {
            NativeToManaged,
            ManagedToNative
        }

        private static (SyntaxToken destination, SyntaxToken source) CopyDirectionToIdentifiers(
            ArrayCopyDirection direction, SyntaxToken managedName, SyntaxToken nativeName
        ) => direction switch
        {
            ArrayCopyDirection.NativeToManaged => (managedName, nativeName),
            ArrayCopyDirection.ManagedToNative => (nativeName, managedName),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

        private StatementSyntax GenerateCopyMemory(CsMarshalBase marshallable, ArrayCopyDirection direction)
        {
            static VariableDeclaratorSyntax FixedDeclaration(SyntaxToken name, ExpressionSyntax source) =>
                VariableDeclarator(
                    name,
                    default,
                    EqualsValueClause(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, source))
                );

            // managed is  __to  when NativeToManaged, __from when ManagedToNative
            //  native is __from when NativeToManaged,  __to  when ManagedToNative
            var (managed, native) = CopyDirectionToIdentifiers(
                direction, Identifier(ToIdentifier), Identifier(FromIdentifier)
            );

            return FixedStatement(
                VariableDeclaration(
                    VoidPtrType,
                    SeparatedList(
                        new[]
                        {
                            FixedDeclaration(
                                managed,
                                ElementAccessExpression(
                                    IdentifierName(marshallable.Name),
                                    BracketedArgumentList(SingletonSeparatedList(Argument(ZeroLiteral)))
                                )
                            ),
                            FixedDeclaration(native, GetMarshalStorageLocation(marshallable))
                        }
                    )
                ),
                GenerateCopyMemoryInvocation(
                    BinaryExpression(
                        SyntaxKind.MultiplyExpression,
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(marshallable.ArrayDimensionValueUnsigned)
                        ),
                        SizeOf(marshallable.MarshalType)
                    ),
                    castTo: false, castFrom: false
                )
            );
        }

        private StatementSyntax GenerateCopyBlock(CsMarshalCallableBase parameter, ArrayCopyDirection direction)
        {
            var arrayIdentifier = IdentifierName(parameter.Name);
            var marshalStorage = GetMarshalStorageLocationIdentifier(parameter);
            var fixedName = Identifier($"{marshalStorage}_");

            var (destination, source) = CopyDirectionToIdentifiers(direction, fixedName, marshalStorage);

            return FixedStatement(
                VariableDeclaration(
                    VoidPtrType,
                    SingletonSeparatedList(VariableDeclarator(fixedName, default, EqualsValueClause(arrayIdentifier)))
                ),
                GenerateCopyMemoryInvocation(
                    IntPtrArgumentWithOptionalCast(IdentifierName(destination), true),
                    IntPtrArgumentWithOptionalCast(IdentifierName(source), true),
                    BinaryExpression(
                        SyntaxKind.MultiplyExpression,
                        GeneratorHelpers.CastExpression(TypeUInt32, GeneratorHelpers.LengthExpression(arrayIdentifier)),
                        SizeOf(parameter.PublicType)
                    )
                )
            );
        }
    }
}
