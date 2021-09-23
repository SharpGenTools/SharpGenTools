using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class StatementSingleCodeGeneratorBase<T> : StatementCodeGeneratorBase<T>,
                                                                  ISingleCodeGenerator<T, StatementSyntax>
        where T : CsBase
    {
        protected StatementSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        public abstract StatementSyntax GenerateCode(T csElement);
    }
}