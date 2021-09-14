using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class LengthRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
        public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            var lengthExpression = GeneratorHelpers.LengthExpression(IdentifierName(publicElement.Name));
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(relatedElement.Name),
                    GenerateNullCheckIfNeeded(
                        publicElement,
                        relatedElement is CsMarshalCallableBase lengthStorage
                            ? GeneratorHelpers.CastExpression(
                                ReverseCallablePrologCodeGenerator.GetPublicType(lengthStorage),
                                lengthExpression
                            )
                            : lengthExpression,
                        DefaultLiteral
                    )
                )
            );
        }

        public static StatementSyntax GenerateNativeToManaged(CsMarshalBase publicElement,
                                                              CsMarshalBase relatedElement) => ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(publicElement.Name),
                ObjectCreationExpression(
                    ArrayType(
                        ParseTypeName(publicElement.PublicType.QualifiedName),
                        SingletonList(
                            ArrayRankSpecifier(
                                SingletonSeparatedList<ExpressionSyntax>(IdentifierName(relatedElement.Name))
                            )
                        )
                    )
                )
            )
        );

        public LengthRelationMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
