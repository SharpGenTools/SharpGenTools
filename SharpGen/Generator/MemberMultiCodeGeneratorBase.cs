using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class MemberMultiCodeGeneratorBase<T> : MemberCodeGeneratorBase<T>,
                                                              IMultiCodeGenerator<T, MemberDeclarationSyntax>
        where T : CsBase
    {
        protected MemberMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement);
    }
}