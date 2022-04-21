using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ResultConstantCodeGenerator : MemberSingleCodeGeneratorBase<CsResultConstant>
{
    public ResultConstantCodeGenerator(Ioc ioc) : base(ioc)
    {
    }

    public override MemberDeclarationSyntax GenerateCode(CsResultConstant csElement) => AddDocumentationTrivia(
        FieldDeclaration(
                VariableDeclaration(
                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(csElement.Name))
                           .WithInitializer(
                                EqualsValueClause(
                                    ImplicitObjectCreationExpression()
                                       .AddArgumentListArguments(Argument(ParseExpression(csElement.Value)))
                                )
                            )
                    )
                )
            )
           .WithModifiers(csElement.VisibilityTokenList),
        csElement
    );
}