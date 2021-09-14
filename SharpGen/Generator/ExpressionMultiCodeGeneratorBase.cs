using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class ExpressionMultiCodeGeneratorBase<T> : StatementCodeGeneratorBase<T>,
                                                                  IMultiCodeGenerator<T, StatementSyntax>,
                                                                  IMultiCodeGenerator<T, ExpressionSyntax>
        where T : CsBase
    {
        protected ExpressionMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        IEnumerable<StatementSyntax> IMultiCodeGenerator<T, StatementSyntax>.GenerateCode(T csElement) =>
            Generate(csElement)
               .Where(static x => x != null)
               .Select(SyntaxFactory.ExpressionStatement);

        IEnumerable<ExpressionSyntax> IMultiCodeGenerator<T, ExpressionSyntax>.GenerateCode(T csElement) =>
            Generate(csElement);

        protected abstract IEnumerable<ExpressionSyntax> Generate(T csElement);
    }
}