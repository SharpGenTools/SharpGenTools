using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ResultRegistrationCodeGenerator : ExpressionSingleCodeGeneratorBase<CsResultConstant>
{
    public ResultRegistrationCodeGenerator(Ioc ioc) : base(ioc)
    {
    }

    protected override ExpressionSyntax Generate(CsResultConstant csElement)
    {
        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GlobalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                IdentifierName("Register")
            ),
            ArgumentList(
                SeparatedList(
                    new[]
                    {
                        Argument(ParseExpression(csElement.Value)),
                        LiteralArgument(csElement.Module),
                        LiteralArgument(csElement.CppElementName),
                        LiteralArgument(csElement.Name)
                    }
                )
            )
        );

        static ArgumentSyntax LiteralArgument(string value) =>
            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value)));
    }
}