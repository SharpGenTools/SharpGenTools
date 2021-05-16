using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class FallbackFieldMarshaller : FieldMarshallerBase
    {
        protected override bool CanMarshal(CsField csField) => true;

        protected override bool AllowNativeCleanup => true;

        protected override StatementSyntax GenerateManagedToNative(CsField csField, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csField),
                    IdentifierName(csField.Name)
                )
            );

        protected override StatementSyntax GenerateNativeToManaged(CsField csField, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csField.Name),
                    GetMarshalStorageLocation(csField)
                )
            );

        public FallbackFieldMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
