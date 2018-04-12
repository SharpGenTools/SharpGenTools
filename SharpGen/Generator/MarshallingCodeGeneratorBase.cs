using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    class MarshallingCodeGeneratorBase
    {
        private readonly GlobalNamespaceProvider globalNamespace;

        protected MarshallingCodeGeneratorBase(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        protected StatementSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable, StatementSyntax statement)
        {
            if (marshallable.IsOptional && (marshallable.IsArray || marshallable.IsInterface || marshallable.IsNullableStruct || marshallable.IsStructClass))
            {
                return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(marshallable.Name),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                statement);
            }
            return statement;
        }

        protected ExpressionSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable, ExpressionSyntax expression, ExpressionSyntax nullAlternative)
        {
            if (marshallable.IsOptional && (marshallable.IsArray || marshallable.IsInterface || marshallable.IsNullableStruct || marshallable.IsStructClass))
            {
                return ConditionalExpression(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        IdentifierName(marshallable.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        nullAlternative,
                        expression);
            }
            return expression;
        }

        protected StatementSyntax LoopThroughArrayParameter(
            CsMarshalBase marshallable,
            Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> loopBodyFactory,
            string variableName = "i")
        {
            var element = ElementAccessExpression(
                                                IdentifierName(marshallable.Name),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName(variableName)))));
            var nativeElement = ElementAccessExpression(
                                                ParenthesizedExpression(GetMarshalStorageLocation(marshallable)),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName(variableName)))));

            return GenerateNullCheckIfNeeded(marshallable,
                ForStatement(loopBodyFactory(element, nativeElement))
                .WithDeclaration(
                    VariableDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(variableName))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0)))))))
                .WithCondition(
                    BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        IdentifierName(variableName),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(marshallable.Name),
                            IdentifierName("Length"))))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PrefixUnaryExpression(
                            SyntaxKind.PreIncrementExpression,
                            IdentifierName(variableName)))));
        }

        protected StatementSyntax CreateMarshalStructStatement(
            CsMarshalBase marshallable,
            string marshalMethod,
            ExpressionSyntax publicElementExpr,
            ExpressionSyntax marshalElementExpr)
        {
            if (marshallable.IsStaticMarshal)
            {
                return GenerateNullCheckIfNeeded(marshallable,
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            ParseTypeName(marshallable.PublicType.QualifiedName),
                            IdentifierName(marshalMethod)),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(publicElementExpr)
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                    Argument(marshalElementExpr)
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                })))));
            }
            else
            {
                return GenerateNullCheckIfNeeded(marshallable,
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            publicElementExpr,
                            IdentifierName(marshalMethod)),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(marshalElementExpr)
                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
            }
        }

        protected StatementSyntax GenerateCopyMemory(CsMarshalBase marshallable, bool copyFromNative, bool useIntermediate = false)
        {
            return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SeparatedList(
                        new[]
                        {
                            VariableDeclarator(copyFromNative ? "__to": "__from")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(
                                        SyntaxKind.AddressOfExpression,
                                        ElementAccessExpression(
                                            useIntermediate ?
                                                IdentifierName(marshallable.IntermediateMarshalName)
                                                : IdentifierName(marshallable.Name)
                                        )
                                        .WithArgumentList(
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            Literal(0))))))))),
                            VariableDeclarator(copyFromNative ? "__from" : "__to")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    GetMarshalStorageLocation(marshallable))))
                        })
                    ),
                GenerateCopyMemoryInvocation(
                    BinaryExpression(SyntaxKind.MultiplyExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(marshallable.ArrayDimensionValue)),
                    SizeOfExpression(ParseTypeName(marshallable.MarshalType.QualifiedName))
                )));
        }

        protected StatementSyntax GenerateAnsiStringToArray(CsMarshalBase marshallable)
        {
            return Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier($"{marshallable.Name}_length"))
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        globalNamespace.GetTypeNameSyntax(BuiltinType.Math),
                                        IdentifierName("Min")),
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(
                                                        BinaryExpression(SyntaxKind.CoalesceExpression,
                                                                ConditionalAccessExpression(
                                                                    IdentifierName(marshallable.Name),
                                                                    MemberBindingExpression(IdentifierName("Length"))),
                                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
                                                    Argument(
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                        Literal(marshallable.ArrayDimensionValue - 1)))
                                                }
                                            )
                                    ))))))),
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName("System.IntPtr"),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier("__from"))
                                    .WithInitializer(EqualsValueClause(
                                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                                            IdentifierName("StringToHGlobalAnsi")))
                                        .WithArgumentList(
                                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(marshallable.Name)))))))))),
                    FixedStatement(
                        VariableDeclaration(
                            PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator("__to")
                                        .WithInitializer(EqualsValueClause(
                                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            GetMarshalStorageLocation(marshallable)))))
                            ),
                        Block(
                            GenerateCopyMemoryInvocation(IdentifierName($"{marshallable.Name}_length")),
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    ElementAccessExpression(IdentifierName("__to"),
                                        BracketedArgumentList(
                                            SingletonSeparatedList(
                                                Argument(IdentifierName($"{marshallable.Name}_length"))))),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))),
                    ExpressionStatement(InvocationExpression(
                                            ParseExpression("System.Runtime.InteropServices.Marshal.FreeHGlobal"),
                                            ArgumentList(SingletonSeparatedList(
                                                Argument(IdentifierName($"__from"))))))
                );
        }

        protected StatementSyntax GenerateStringToArray(CsMarshalBase marshallable)
        {
            return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                    SeparatedList(
                        new[]
                        {
                            VariableDeclarator("__from")
                                .WithInitializer(EqualsValueClause(IdentifierName(marshallable.Name))),
                            VariableDeclarator("__to")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    GetMarshalStorageLocation(marshallable))))
                        })
                    ),
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier($"{marshallable.Name}_length"))
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        globalNamespace.GetTypeNameSyntax(BuiltinType.Math),
                                        IdentifierName("Min")),
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(
                                                        BinaryExpression(SyntaxKind.MultiplyExpression,
                                                            ParenthesizedExpression(
                                                                BinaryExpression(SyntaxKind.CoalesceExpression,
                                                                ConditionalAccessExpression(
                                                                    IdentifierName(marshallable.Name),
                                                                    MemberBindingExpression(IdentifierName("Length"))),
                                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2))
                                                        )),
                                                    Argument(
                                                        BinaryExpression(SyntaxKind.MultiplyExpression,
                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(marshallable.ArrayDimensionValue - 1)),
                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2))))
                                                }
                                            )
                                    ))))))),
                    GenerateCopyMemoryInvocation(IdentifierName($"{marshallable.Name}_length")),
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            ElementAccessExpression(IdentifierName("__to"),
                                BracketedArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName($"{marshallable.Name}_length"))))),
                            LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal('\0'))))));
        }

        private ExpressionStatementSyntax GenerateCopyMemoryInvocation(ExpressionSyntax numBytesExpression)
        {
            return ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        globalNamespace.GetTypeNameSyntax(WellKnownName.MemoryHelpers),
                                        IdentifierName("CopyMemory")),
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                    Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__to"))),
                                    Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__from"))),
                                    Argument(numBytesExpression)
                                            }
                                        ))));
        }

        protected SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                    return Identifier($"{marshallable.Name}_");
                case CsField _:
                    throw new ArgumentException("Marshal storage location for a field cannot be represented by a token.", nameof(marshallable));
                case CsReturnValue returnValue:
                    return Identifier(returnValue.MarshalStorageLocation);
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected ExpressionSyntax GetMarshalStorageLocation(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                case CsReturnValue _:
                    return IdentifierName(GetMarshalStorageLocationIdentifier(marshallable));
                case CsField _:
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("@ref"),
                        IdentifierName(marshallable.Name));
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected StatementSyntax NotImplemented(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(
                    ParseTypeName("System.NotImplementedException"))
                .WithArgumentList(
                    ArgumentList(
                        message == null ? default
                        : SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(message)))))));
        }
    }
}
