using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace SharpGen.Generator.Marshallers
{
    class FallbackFieldMarshaller : MarshallerBase, IMarshaller
    {
        public FallbackFieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement is CsField;
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
            if (field.IsBitField)
            {
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
            return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    IdentifierName(csElement.Name)));
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
            if (field.IsBitField)
            {
                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.IntermediateMarshalName),
                    GetMarshalStorageLocation(csElement)));
            }
            return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.Name),
                    GetMarshalStorageLocation(csElement)));
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }
    }
}
