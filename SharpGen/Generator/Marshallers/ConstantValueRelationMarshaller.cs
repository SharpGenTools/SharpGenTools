using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class ConstantValueRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
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

        public ConstantValueRelationMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
