using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class GuidConstantCodeGenerator : MemberSingleCodeGeneratorBase<CsGuidConstant>
    {
        public GuidConstantCodeGenerator(Ioc ioc) : base(ioc)
        {
        }

        public override MemberDeclarationSyntax GenerateCode(CsGuidConstant csElement)
        {
            static ArgumentSyntax ArgumentSelector(SyntaxToken x) =>
                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, x));

            var guidArguments = GuidConstantRoslynGenerator.GuidToTokens(csElement.Value)
                                                           .Select(ArgumentSelector)
                                                           .ToArray();

            return AddDocumentationTrivia(
                FieldDeclaration(
                        VariableDeclaration(
                            GlobalNamespace.GetTypeNameSyntax(BuiltinType.Guid),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(csElement.Name))
                                   .WithInitializer(
                                        EqualsValueClause(
                                            ImplicitObjectCreationExpression()
                                               .AddArgumentListArguments(guidArguments)
                                        ))
                            )
                        )
                    )
                   .WithModifiers(csElement.VisibilityTokenList),
                csElement
            );
        }
    }
}