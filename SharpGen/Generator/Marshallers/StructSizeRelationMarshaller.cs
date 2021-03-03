using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class StructSizeRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
        public StructSizeRelationMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(relatedElement),
                    SizeOfExpression(IdentifierName("__Native"))));
        }
    }
}
