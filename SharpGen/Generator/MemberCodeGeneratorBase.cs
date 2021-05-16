using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using SharpGen.Transform;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal abstract class MemberCodeGeneratorBase<T> : CodeGeneratorBase, IMultiCodeGenerator<T, MemberDeclarationSyntax>
        where T : CsBase
    {
        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement);

        private SyntaxTriviaList GenerateDocumentationTrivia(CsBase csElement)
        {
            var docItems = DocumentationLinker.GetDocItems(ExternalDocReader, csElement);

            StringBuilder builder = new();
            foreach (var docItem in docItems)
                builder.AppendLine($"/// {docItem}");
            builder.AppendLine();

            return ParseLeadingTrivia(builder.ToString());
        }

        protected TMember AddDocumentationTrivia<TMember>(TMember member, CsBase csElement) where TMember : MemberDeclarationSyntax
        {
            return member.WithLeadingTrivia(GenerateDocumentationTrivia(csElement));
        }

        protected MemberCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }
    }
}
