using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class StringMarshaller : MarshallerBase, IMarshaller
    {
        private const string FromIdentifier = "__from";
        private const string ToIdentifier = "__to";

        public StringMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        private static TypeSyntax StringType { get; } = ParseTypeName("System.String");

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
                if (!csElement.IsWideChar)
                {
                    return GenerateAnsiStringToArray(csElement);
                }

                if (!singleStackFrame)
                {
                    return GenerateStringToArray(csElement);
                }

                return null;
            }

            // Variable-length string represented as a pointer.

            if (!csElement.IsWideChar || !singleStackFrame)
            {
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                                IdentifierName(
                                    csElement.IsWideChar
                                        ? nameof(Marshal.StringToHGlobalUni)
                                        : nameof(Marshal.StringToHGlobalAnsi)
                                )
                            ),
                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(csElement.Name))))
                        )
                    )
                );
            }

            return null;
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            if (!csElement.IsWideChar || csElement.UsedAsReturn)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        IntPtrType,
                        SingletonSeparatedList(VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))
                    )
                );
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.IsOut
                ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement))
                : CastExpression(VoidPtrType, GetMarshalStorageLocation(csElement))
        );

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (!csElement.IsWideChar || !singleStackFrame)
            {
                return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                            IdentifierName(nameof(Marshal.FreeHGlobal))
                        ),
                        ArgumentList(SingletonSeparatedList(Argument(GetMarshalStorageLocation(csElement))))
                    )
                );
            }
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            MemberAccessExpressionSyntax PtrToString(NameSyntax implName) =>
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    implName,
                    IdentifierName(
                        csElement.IsWideChar ? nameof(Marshal.PtrToStringUni) : nameof(Marshal.PtrToStringAnsi)
                    )
                );

            if (csElement.IsArray) // Fixed-length character array
            {
                if (csElement.IsWideChar && singleStackFrame)
                    return null;

                const string ptrName = "__ptr";

                return FixedStatement(
                    VariableDeclaration(
                        VoidPtrType,
                        SingletonSeparatedList(
                            VariableDeclarator(ptrName)
                               .WithInitializer(
                                    EqualsValueClause(
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                            GetMarshalStorageLocation(csElement)
                                        )
                                    )
                                )
                        )
                    ),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(csElement.Name),
                            InvocationExpression(
                                PtrToString(GlobalNamespace.GetTypeNameSyntax(WellKnownName.StringHelpers)),
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(CastExpression(IntPtrType, IdentifierName(ptrName))),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(csElement.ArrayDimensionValue - 1)
                                                )
                                            )
                                        }
                                    )
                                )
                            )
                        )
                    )
                );
            }

            // Variable-length string represented as a pointer.
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.Name),
                    InvocationExpression(
                        PtrToString(GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal)),
                        ArgumentList(SingletonSeparatedList(Argument(GetMarshalStorageLocation(csElement))))
                    )
                )
            );
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            if (csElement.IsWideChar)
            {
                return FixedStatement(
                    VariableDeclaration(
                        PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                GetMarshalStorageLocationIdentifier(csElement),
                                null,
                                EqualsValueClause(IdentifierName(csElement.Name))
                            )
                        )
                    ),
                    EmptyStatement()
                );
            }

            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => IntPtrType;

        private static string LengthIdentifier(CsMarshalBase marshallable) => $"{marshallable.Name}_length";

        private StatementSyntax GenerateAnsiStringToArray(CsMarshalBase marshallable)
        {
            var lengthIdentifier = LengthIdentifier(marshallable);

            return Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(lengthIdentifier))
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Math),
                                        IdentifierName(nameof(Math.Min))),
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
                            IntPtrType,
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(FromIdentifier))
                                    .WithInitializer(EqualsValueClause(
                                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                                            IdentifierName(nameof(Marshal.StringToHGlobalAnsi))))
                                        .WithArgumentList(
                                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(marshallable.Name)))))))))),
                    FixedStatement(
                        VariableDeclaration(
                            PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(ToIdentifier)
                                        .WithInitializer(EqualsValueClause(
                                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            GetMarshalStorageLocation(marshallable)))))
                            ),
                        Block(
                            GenerateCopyMemoryInvocation(IdentifierName(lengthIdentifier)),
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    ElementAccessExpression(IdentifierName(ToIdentifier),
                                        BracketedArgumentList(
                                            SingletonSeparatedList(
                                                Argument(IdentifierName(lengthIdentifier))))),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))),
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Marshal),
                            IdentifierName(nameof(Marshal.FreeHGlobal))),
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName(FromIdentifier))))))
                );
        }

        private StatementSyntax GenerateStringToArray(CsMarshalBase marshallable)
        {
            var lengthIdentifier = LengthIdentifier(marshallable);

            return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                    SeparatedList(
                        new[]
                        {
                            VariableDeclarator(FromIdentifier)
                                .WithInitializer(EqualsValueClause(IdentifierName(marshallable.Name))),
                            VariableDeclarator(ToIdentifier)
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
                                VariableDeclarator(Identifier(lengthIdentifier))
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Math),
                                        IdentifierName(nameof(Math.Min))),
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
                    GenerateCopyMemoryInvocation(IdentifierName(lengthIdentifier)),
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            ElementAccessExpression(IdentifierName(ToIdentifier),
                                BracketedArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName(lengthIdentifier))))),
                            LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal('\0'))))));
        }
    }
}
