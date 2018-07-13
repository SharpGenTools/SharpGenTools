using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;
using System;

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
            // TODO: Reverse-callback support?
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
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }
    }
}
