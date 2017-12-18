using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGen.Generator
{
    abstract class MemberCodeGeneratorBase<T> : ICodeGenerator<T, MemberDeclarationSyntax>
        where T : CsBase
    {
        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement);
        
        protected DocumentationCommentTriviaSyntax GenerateDocumentationTrivia(CsBase csElement)
        {
            return DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SingletonList<XmlNodeSyntax>(XmlText()
                        .WithTextTokens(
                            TokenList(Manager.GetDocItems(csElement).SelectMany(item =>
                                new[]{
                                    XmlTextNewLine("\n", true),
                                    XmlTextLiteral(item),
                                    XmlTextNewLine("\n", true)
                                })))));
        }

        private TransformManager Manager;
    }
}
