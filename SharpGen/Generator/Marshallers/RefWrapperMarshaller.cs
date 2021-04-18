using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class RefWrapperMarshaller : MarshallerBase, IMarshaller
    {
        private readonly IMarshaller implementation;

        public RefWrapperMarshaller(GlobalNamespaceProvider globalNamespace, IMarshaller implementation)
            : base(globalNamespace)
        {
            this.implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }

        public static bool IsApplicable(CsMarshalBase csElement) =>
            csElement is CsMarshalCallableBase {IsLocalByRef: true, IsArray: false};

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
            implementation.GenerateManagedToNativeProlog(csElement);

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            implementation.GenerateNativeToManagedExtendedProlog(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            var statement = implementation.GenerateManagedToNative(csElement, singleStackFrame);

            return csElement switch
            {
                CsParameter {IsOptional: true} parameter => GenerateManagedToNativeForOptional(parameter, statement),
                _ => statement
            };
        }

        private static StatementSyntax GenerateManagedToNativeForOptional(CsParameter parameter,
                                                                          StatementSyntax statement)
        {
            var refIdentifier = IdentifierName(GetRefLocationIdentifier(parameter));

            StatementSyntaxList statements = new()
            {
                statement,
                IfStatement(
                    BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Unsafe),
                                IdentifierName(nameof(Unsafe.AsPointer))
                            ),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(refIdentifier).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                )
                            )
                        ),
                        LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword))
                    ),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            refIdentifier,
                            IdentifierName(parameter.Name)
                        )
                    )
                )
            };

            return statements.ToStatement();
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            implementation.GenerateNativeToManaged(csElement, singleStackFrame);

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            implementation.GenerateNativeArgument(csElement);

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            implementation.GenerateManagedArgument(csElement);

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            implementation.GenerateManagedParameter(csElement);

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            implementation.GenerateNativeCleanup(csElement, singleStackFrame);

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => implementation.GeneratePin(csElement);

        public bool CanMarshal(CsMarshalBase csElement) => implementation.CanMarshal(csElement);

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) =>
            implementation.GeneratesMarshalVariable(csElement);

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            implementation.GetMarshalTypeSyntax(csElement);
    }
}