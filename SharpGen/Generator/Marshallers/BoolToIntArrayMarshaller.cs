using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class BoolToIntArrayMarshaller : MarshallerBase, IMarshaller
    {
        public BoolToIntArrayMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsBoolToInt && csElement.IsArray;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            return Argument(IdentifierName(csElement.Name));
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            return GenerateManagedArrayParameter(csElement);
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            // TODO: Reverse-callback support?
            if (singleStackFrame)
            {
                return GenerateNullCheckIfNeeded(csElement,
                    ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                    IdentifierName("ConvertToIntArray")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                                    Argument(IdentifierName(csElement.Name)),
                                                    Argument(GetMarshalStorageLocation(csElement))
                                        }
                            )))));
            }
            else
            {
                return GenerateNullCheckIfNeeded(csElement,
                    FixedStatement(
                        VariableDeclaration(
                            PointerType(
                                ParseTypeName(csElement.MarshalType.QualifiedName)))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("__ptr"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                           GetMarshalStorageLocation(csElement)))))),
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                    IdentifierName("ConvertToIntArray")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                                    Argument(IdentifierName(csElement.Name)),
                                                    Argument(IdentifierName("__ptr"))
                                        }
                            ))))));
            }
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
               VariableDeclaration(
                   PointerType(
                       ParseTypeName(csElement.MarshalType.QualifiedName)),
                   SingletonSeparatedList(
                       VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))));
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                            CastExpression(
                                PointerType(
                                    ParseTypeName(csElement.MarshalType.QualifiedName)),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))));
            yield return GenerateNullCheckIfNeeded(csElement,
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PointerType(ParseTypeName(csElement.MarshalType.QualifiedName)),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(csElement.IntermediateMarshalName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        StackAllocArrayCreationExpression(
                                            ArrayType(
                                                ParseTypeName(csElement.MarshalType.QualifiedName),
                                                SingletonList(
                                                    ArrayRankSpecifier(
                                                        SingletonSeparatedList<ExpressionSyntax>(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(csElement.Name),
                                                                IdentifierName("Length")))))))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                            IdentifierName(csElement.IntermediateMarshalName)))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(GetMarshalStorageLocation(csElement));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (singleStackFrame)
            {
                return GenerateNullCheckIfNeeded(csElement,
                    ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                    IdentifierName("ConvertToBoolArray")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(GetMarshalStorageLocation(csElement)),
                                            Argument(IdentifierName(csElement.Name))
                                        }
                            )))));
            }
            else if (csElement is CsField)
            {
                return GenerateNullCheckIfNeeded(csElement,
                    FixedStatement(
                        VariableDeclaration(
                            PointerType(
                                ParseTypeName(csElement.MarshalType.QualifiedName)))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("__ptr"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                           GetMarshalStorageLocation(csElement)))))),
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                    IdentifierName("ConvertToBoolArray")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(IdentifierName("__ptr")),
                                            Argument(IdentifierName(csElement.Name))
                                        }
                            ))))));
            }
            else // Reverse-callbacks
            {
                return GenerateNullCheckIfNeeded(csElement,
                    ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                    IdentifierName("ConvertToBoolArray")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(GetMarshalStorageLocation(csElement)),
                                            Argument(IdentifierName(csElement.Name))
                                        }
                            ))))
                );
            }
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return true;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return PointerType(ParseTypeName(csElement.MarshalType.QualifiedName));
        }
    }
}
