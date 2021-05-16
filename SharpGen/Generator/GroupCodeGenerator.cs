using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class GroupCodeGenerator : MemberCodeGeneratorBase<CsGroup>
    {
        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsGroup csElement)
        {
            var members = csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var));
            members = members.Concat(csElement.Functions.SelectMany(func => Generators.Function.GenerateCode(func)));

            yield return AddDocumentationTrivia(
                ClassDeclaration(Identifier(csElement.Name))
                   .WithModifiers(csElement.VisibilityTokenList.Add(Token(SyntaxKind.PartialKeyword)))
                   .WithMembers(List(members)),
                csElement
            );
        }

        public GroupCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
