using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class StatementPlatformSingleCodeGeneratorBase<T>
    : StatementCodeGeneratorBase<T>, IPlatformSingleCodeGenerator<T, StatementSyntax>
    where T : CsBase
{
    protected StatementPlatformSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);
    public abstract StatementSyntax GenerateCode(T csElement, PlatformDetectionType platform);
}