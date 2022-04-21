using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ExpressionConstantCodeGenerator : MemberSingleCodeGeneratorBase<CsExpressionConstant>
{
    public ExpressionConstantCodeGenerator(Ioc ioc) : base(ioc)
    {
    }

    public override MemberDeclarationSyntax GenerateCode(CsExpressionConstant csElement)
    {
        var typeName = ParseTypeName(csElement.Type.QualifiedName);
        return AddDocumentationTrivia(
            FieldDeclaration(
                    VariableDeclaration(
                        typeName,
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(csElement.Name))
                               .WithInitializer(
                                    EqualsValueClause(
                                        CheckedExpression(
                                            SyntaxKind.UncheckedExpression,
                                            GeneratorHelpers.CastExpression(typeName, ParseExpression(csElement.Value))
                                        )
                                    )
                                )
                        )
                    )
                )
               .WithModifiers(csElement.VisibilityTokenList),
            csElement
        );
    }
}