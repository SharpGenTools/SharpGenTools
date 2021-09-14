using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class GroupCodeGenerator : MemberSingleCodeGeneratorBase<CsGroup>
    {
        public GroupCodeGenerator(Ioc ioc) : base(ioc)
        {
        }

        public override MemberDeclarationSyntax GenerateCode(CsGroup csElement)
        {
            var list = NewMemberList;
            list.AddRange(csElement.ExpressionConstants, Generators.ExpressionConstant);
            list.AddRange(csElement.GuidConstants, Generators.GuidConstant);
            list.AddRange(csElement.ResultConstants, Generators.ResultConstant);
            list.AddRange(csElement.Functions, Generators.Function);

            return AddDocumentationTrivia(
                ClassDeclaration(Identifier(csElement.Name))
                   .WithModifiers(csElement.VisibilityTokenList.Add(Token(SyntaxKind.PartialKeyword)))
                   .WithMembers(List(list)),
                csElement
            );
        }
    }
}