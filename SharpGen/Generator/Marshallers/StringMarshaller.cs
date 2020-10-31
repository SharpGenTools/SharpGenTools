using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class StringMarshaller : MarshallerBase, IMarshaller
    {
        public StringMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        private TypeSyntax StringType { get; } = ParseTypeName("System.String");

        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsString;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            var arg = Argument(IdentifierName(csElement.Name));

            if (csElement.IsOut)
            {
                arg = arg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
            }

            return arg;
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name))
                .WithType(StringType);

            if (csElement.IsOut)
            {
                param = param.AddModifiers(Token(SyntaxKind.OutKeyword));
            }

            return param;
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (csElement.IsArray) // Fixed-length character array
            {
                if (csElement.IsWideChar && !singleStackFrame)
                {
                    return GenerateStringToArray(csElement);
                }
                else if (!csElement.IsWideChar)
                {
                    return GenerateAnsiStringToArray(csElement);
                }
                return null;
            }
            else // Variable-length string represented as a pointer.
            {
                if (!csElement.IsWideChar || !singleStackFrame)
                {
                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    GetMarshalStorageLocation(csElement),
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                                            IdentifierName("StringToHGlobal" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(csElement.Name)))))));
                }
                return null;
            }
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            if (!csElement.IsWideChar || csElement.UsedAsReturn)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        IntPtrType,
                        SingletonSeparatedList(
                            VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))));
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            if (csElement.IsOut)
            {
                return Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement)));
            }

            return Argument(CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    GetMarshalStorageLocation(csElement)));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (!csElement.IsWideChar || !singleStackFrame)
            {
                return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                            IdentifierName("FreeHGlobal")),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(GetMarshalStorageLocation(csElement))))));
            }
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (csElement.IsArray) // Fixed-length character array
            {
                if (!csElement.IsWideChar || !singleStackFrame)
                {
                    return FixedStatement(
                        VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator("__ptr")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                        GetMarshalStorageLocation(csElement))
                                )))),
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                globalNamespace.GetTypeNameSyntax(WellKnownName.StringHelpers),
                                IdentifierName("PtrToString" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                            ArgumentList(SeparatedList(
                                new[]
                                {
                                        Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__ptr"))),
                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.ArrayDimensionValue - 1)))
                                }
                                ))))));
                }
                return null;
            }

            if (!csElement.IsWideChar || !singleStackFrame) // Variable-length string represented as a pointer.
            {
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(csElement.Name),
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                                        IdentifierName("PtrToString" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                                    ArgumentList(SingletonSeparatedList(
                                        Argument(
                                            GetMarshalStorageLocation(csElement)))))));
            }
            return null;
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            return Enumerable.Empty<StatementSyntax>();
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            if (csElement.IsWideChar)
            {
                return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)).WithInitializer(EqualsValueClause(
                            IdentifierName(csElement.Name)
                            )))), EmptyStatement());
            }

            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return true;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return IntPtrType;
        }

        private StatementSyntax GenerateAnsiStringToArray(CsMarshalBase marshallable)
        {
            return Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier($"{marshallable.Name}_length"))
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Math),
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
                                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
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
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                            IdentifierName("FreeHGlobal")),
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName($"__from"))))))
                );
        }

        private StatementSyntax GenerateStringToArray(CsMarshalBase marshallable)
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
                                        GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Math),
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
    }
}
