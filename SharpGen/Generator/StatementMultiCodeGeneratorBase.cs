using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class StatementMultiCodeGeneratorBase<T> : StatementCodeGeneratorBase<T>,
                                                                 IMultiCodeGenerator<T, StatementSyntax>
        where T : CsBase
    {
        protected StatementMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        public abstract IEnumerable<StatementSyntax> GenerateCode(T csElement);
    }
}