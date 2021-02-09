using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class FallbackFieldMarshaller : MarshallerBase, IMarshaller
    {
        public FallbackFieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement is CsField;

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    IdentifierName(csElement.Name)
                )
            );

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.Name),
                    GetMarshalStorageLocation(csElement)
                )
            );

        #region Non-supported operations

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => throw new NotSupportedException();

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => throw new NotSupportedException();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => throw new NotSupportedException();

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) => throw new NotSupportedException();

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) => throw new NotSupportedException();

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

        #endregion
    }
}
