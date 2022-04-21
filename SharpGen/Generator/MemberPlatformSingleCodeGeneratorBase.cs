using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class MemberPlatformSingleCodeGeneratorBase<T>
    : MemberCodeGeneratorBase<T>, IPlatformSingleCodeGenerator<T, MemberDeclarationSyntax>
    where T : CsBase
{
    protected MemberPlatformSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);
    public abstract MemberDeclarationSyntax GenerateCode(T csElement, PlatformDetectionType platform);
}