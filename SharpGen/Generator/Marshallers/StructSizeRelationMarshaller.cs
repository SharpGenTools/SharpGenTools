using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal sealed class StructSizeRelationMarshaller : MarshallerBase, IRelationMarshaller
{
    public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement) =>
        ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                GetMarshalStorageLocation(relatedElement),
                SizeOfExpression(IdentifierName("__Native"))
            )
        );

    public StructSizeRelationMarshaller(Ioc ioc) : base(ioc)
    {
    }
}