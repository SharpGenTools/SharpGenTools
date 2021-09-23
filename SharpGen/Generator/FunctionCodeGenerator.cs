using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal sealed class FunctionCodeGenerator : MemberMultiCodeGeneratorBase<CsFunction>
    {
        public FunctionCodeGenerator(Ioc ioc) : base(ioc)
        {
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsFunction csElement)
        {
            var list = NewMemberList;
            list.Add(csElement, Generators.Callable);
            list.Add(csElement, Generators.FunctionImport);
            return list;
        }
    }
}