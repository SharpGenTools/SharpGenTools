using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class BitfieldMarshaller : MarshallerBase, IMarshaller
    {
        public BitfieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement is CsField {IsBitField: true};

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            var field = (CsField) csElement;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.OrAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    CastExpression(
                        ParseTypeName(csElement.MarshalType.QualifiedName),
                        ParenthesizedExpression(
                            BinaryExpression(
                                SyntaxKind.BitwiseAndExpression,
                                IdentifierName(csElement.IntermediateMarshalName),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(field.BitMask << field.BitOffset)
                                )
                            )
                        )
                    )
                )
            );
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.IntermediateMarshalName),
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
