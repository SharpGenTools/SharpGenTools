using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class LengthRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
        public LengthRelationMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(relatedElement.Name),
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(publicElement.Name),
                    IdentifierName("Length"))));
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(publicElement.Name),
                ObjectCreationExpression(
                    ArrayType(
                        ParseTypeName(publicElement.PublicType.QualifiedName),
                        SingletonList(
                            ArrayRankSpecifier(
                                SingletonSeparatedList<ExpressionSyntax>(
                                    IdentifierName(relatedElement.Name))))))));
        }
    }
}
