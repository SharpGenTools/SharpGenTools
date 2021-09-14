using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class ExpressionPlatformSingleCodeGeneratorBase<T>
        : StatementCodeGeneratorBase<T>,
          IPlatformSingleCodeGenerator<T, StatementSyntax>,
          IPlatformSingleCodeGenerator<T, ExpressionSyntax>
        where T : CsBase
    {
        protected ExpressionPlatformSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);

        ExpressionSyntax IPlatformSingleCodeGenerator<T, ExpressionSyntax>.GenerateCode(
            T csElement, PlatformDetectionType platform
        ) => Generate(csElement, platform);

        StatementSyntax IPlatformSingleCodeGenerator<T, StatementSyntax>.GenerateCode(
            T csElement, PlatformDetectionType platform
        ) => Generate(csElement, platform) is { } value ? SyntaxFactory.ExpressionStatement(value) : null;

        protected abstract ExpressionSyntax Generate(T csElement, PlatformDetectionType platform);
    }
}