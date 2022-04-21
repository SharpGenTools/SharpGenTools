using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class StatementPlatformMultiCodeGeneratorBase<T>
    : StatementCodeGeneratorBase<T>, IPlatformMultiCodeGenerator<T, StatementSyntax>
    where T : CsBase
{
    protected StatementPlatformMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);
    public abstract IEnumerable<StatementSyntax> GenerateCode(T csElement, PlatformDetectionType platform);
}