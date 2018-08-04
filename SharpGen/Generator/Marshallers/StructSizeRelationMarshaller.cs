using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class StructSizeRelationMarshaller : MarshallerBase, IRelationMarshaller
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

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return null;
        }
    }
}
