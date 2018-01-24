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
        private readonly ExternalDocCommentsReader docReader;

        protected MemberCodeGeneratorBase(IDocumentationLinker documentation, ExternalDocCommentsReader docReader)
        {
            docAggregator = documentation;
            this.docReader = docReader;
        }

        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement);
        
        protected DocumentationCommentTriviaSyntax GenerateDocumentationTrivia(CsBase csElement)
        {
            var docItems = docAggregator.GetDocItems(docReader, csElement);

            var builder = new StringBuilder();
            foreach (var docItem in docItems)
            {
                builder.AppendLine($"/// {docItem}");
            }
            builder.AppendLine();

            var tree = CSharpSyntaxTree.ParseText(builder.ToString());
            return (DocumentationCommentTriviaSyntax)tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia[0].GetStructure();
        }

        private readonly IDocumentationLinker docAggregator;
    }
}
