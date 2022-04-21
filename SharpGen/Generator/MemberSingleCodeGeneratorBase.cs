using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class MemberSingleCodeGeneratorBase<T> : MemberCodeGeneratorBase<T>,
                                                           ISingleCodeGenerator<T, MemberDeclarationSyntax>
    where T : CsBase
{
    protected MemberSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract MemberDeclarationSyntax GenerateCode(T csElement);
}