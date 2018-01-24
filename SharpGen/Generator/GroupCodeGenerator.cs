using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class GroupCodeGenerator : MemberCodeGeneratorBase<CsGroup>
    {
        public GroupCodeGenerator(IGeneratorRegistry generators, IDocumentationLinker documentation, ExternalDocCommentsReader reader)
            : base(documentation, reader)
        {
            Generators = generators;
        }

        public IGeneratorRegistry Generators { get; }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsGroup csElement)
        {
            yield return ClassDeclaration(Identifier(csElement.Name))
                .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .WithMembers(
                    List(
                        csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var))
                    ).AddRange(csElement.Functions.SelectMany(func => Generators.Function.GenerateCode(func))))
                .WithLeadingTrivia(Trivia(GenerateDocumentationTrivia(csElement)));
        }
    }
}
