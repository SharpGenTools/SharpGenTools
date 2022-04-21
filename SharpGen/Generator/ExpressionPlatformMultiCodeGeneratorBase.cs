using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class ExpressionPlatformMultiCodeGeneratorBase<T>
    : StatementCodeGeneratorBase<T>,
      IPlatformMultiCodeGenerator<T, StatementSyntax>,
      IPlatformMultiCodeGenerator<T, ExpressionSyntax>
    where T : CsBase
{
    protected ExpressionPlatformMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);

    IEnumerable<StatementSyntax> IPlatformMultiCodeGenerator<T, StatementSyntax>.GenerateCode(
        T csElement, PlatformDetectionType platform
    ) => Generate(csElement, platform)
        .Where(static x => x != null)
        .Select(SyntaxFactory.ExpressionStatement);

    IEnumerable<ExpressionSyntax> IPlatformMultiCodeGenerator<T, ExpressionSyntax>.GenerateCode(
        T csElement, PlatformDetectionType platform
    ) => Generate(csElement, platform);

    protected abstract IEnumerable<ExpressionSyntax> Generate(T csElement, PlatformDetectionType platform);
}