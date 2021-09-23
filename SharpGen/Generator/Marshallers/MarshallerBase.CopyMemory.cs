using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal abstract partial class MarshallerBase
    {
        protected static readonly PredefinedTypeSyntax TypeInt32 = PredefinedType(Token(SyntaxKind.IntKeyword));
        protected static readonly PredefinedTypeSyntax TypeUInt32 = PredefinedType(Token(SyntaxKind.UIntKeyword));

        protected const string FromIdentifier = "__from";
        protected const string ToIdentifier = "__to";

        protected ExpressionStatementSyntax GenerateCopyMemoryInvocation(ExpressionSyntax numBytesExpression,
                                                                         bool castTo = true, bool castFrom = true) =>
            GenerateCopyMemoryInvocation(
                IntPtrArgumentWithOptionalCast(IdentifierName(ToIdentifier), castTo),
                IntPtrArgumentWithOptionalCast(IdentifierName(FromIdentifier), castFrom),
                numBytesExpression
            );

        protected static ExpressionSyntax IntPtrArgumentWithOptionalCast(ExpressionSyntax name, bool cast) =>
            cast ? GeneratorHelpers.CastExpression(IntPtrType, name) : name;

        protected ExpressionStatementSyntax GenerateCopyMemoryInvocation(
            ExpressionSyntax destination, ExpressionSyntax source, ExpressionSyntax numBytesExpression
        ) => ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.MemoryHelpers),
                    IdentifierName("CopyMemory")
                ),
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(destination),
                            Argument(source),
                            Argument(numBytesExpression)
                        }
                    )
                )
            )
        );

        protected ExpressionSyntax SizeOf(TypeSyntax elementType)
        {
            if (elementType is PredefinedTypeSyntax {Keyword: var predefinedType})
            {
                static LiteralExpressionSyntax LiteralSizeOf(uint size) => LiteralExpression(
                    SyntaxKind.NumericLiteralExpression, Literal(size)
                );

                bool CheckKinds(params SyntaxKind[] kinds) => kinds.Any(x => predefinedType.IsKind(x));

                if (CheckKinds(SyntaxKind.ByteKeyword, SyntaxKind.SByteKeyword, SyntaxKind.BoolKeyword))
                    return LiteralSizeOf(1);

                if (CheckKinds(SyntaxKind.ShortKeyword, SyntaxKind.UShortKeyword))
                    return LiteralSizeOf(2);

                if (CheckKinds(SyntaxKind.IntKeyword, SyntaxKind.UIntKeyword, SyntaxKind.FloatKeyword))
                    return LiteralSizeOf(4);

                if (CheckKinds(SyntaxKind.LongKeyword, SyntaxKind.ULongKeyword, SyntaxKind.DoubleKeyword))
                    return LiteralSizeOf(8);

                if (CheckKinds(SyntaxKind.DecimalKeyword))
                    return LiteralSizeOf(16);
            }

            ExpressionSyntax value = elementType switch
            {
                QualifiedNameSyntax qualifiedName when qualifiedName.IsEquivalentTo(GeneratorHelpers.IntPtrType) =>
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, qualifiedName, IdentifierName(nameof(IntPtr.Size))
                    ),
                QualifiedNameSyntax qualifiedName when qualifiedName.IsEquivalentTo(GeneratorHelpers.UIntPtrType) =>
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, qualifiedName, IdentifierName(nameof(UIntPtr.Size))
                    ),
                _ => InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GlobalNamespace.GetTypeNameSyntax(BuiltinType.Unsafe),
                        GenericName(
                            Identifier(nameof(Unsafe.SizeOf)),
                            TypeArgumentList(SingletonSeparatedList(elementType))
                        )
                    )
                )
            };

            return GeneratorHelpers.CastExpression(TypeUInt32, value);
        }

        protected ExpressionSyntax SizeOf(CsTypeBase elementType) => SizeOf(ParseTypeName(elementType.QualifiedName));
    }
}