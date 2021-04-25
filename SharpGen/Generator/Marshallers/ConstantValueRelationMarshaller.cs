using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class ConstantValueRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
        public ConstantValueRelationMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            var relation = relatedElement.Relations.OfType<ConstantValueRelation>().Single();

            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    relatedElement is CsField
                        ? GetMarshalStorageLocation(relatedElement)
                        : IdentifierName(relatedElement.Name),
                    relation.Value
                )
            );
        }
    }
}
