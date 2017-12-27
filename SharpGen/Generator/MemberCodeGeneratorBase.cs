using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    abstract class MemberCodeGeneratorBase<T> : IMultiCodeGenerator<T, MemberDeclarationSyntax>
        where T : CsBase
    {
        protected MemberCodeGeneratorBase(IDocumentationAggregator documentation)
        {
            docAggregator = documentation;
        }

        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement);
        
        protected DocumentationCommentTriviaSyntax GenerateDocumentationTrivia(CsBase csElement)
        {
            return DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SingletonList<XmlNodeSyntax>(XmlText()
                        .WithTextTokens(
                            TokenList(docAggregator.GetDocItems(csElement).SelectMany(item =>
                                new[]{
                                    XmlTextNewLine("\n", true),
                                    XmlTextLiteral(item),
                                    XmlTextNewLine("\n", true)
                                })))));
        }

        private readonly IDocumentationAggregator docAggregator;
    }
}
