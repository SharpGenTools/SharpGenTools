using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator.Marshallers
{
    class BitfieldMarshaller : MarshallerBase, IMarshaller
    {
        public BitfieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement is CsField field && field.IsBitField;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            var field = (CsField)csElement;
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.OrAssignmentExpression,
                GetMarshalStorageLocation(csElement),
                CastExpression(ParseTypeName(csElement.MarshalType.QualifiedName),
                    ParenthesizedExpression(BinaryExpression(SyntaxKind.BitwiseAndExpression,
                        IdentifierName(csElement.IntermediateMarshalName),
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(field.BitMask << field.BitOffset)))))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            throw new InvalidOperationException();
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            var field = (CsField)csElement;
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(csElement.IntermediateMarshalName),
                GetMarshalStorageLocation(csElement)));
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }
    }
}
