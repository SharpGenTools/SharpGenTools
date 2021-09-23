using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class MemberPlatformMultiCodeGeneratorBase<T>
        : MemberCodeGeneratorBase<T>, IPlatformMultiCodeGenerator<T, MemberDeclarationSyntax>
        where T : CsBase
    {
        protected MemberPlatformMultiCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        public abstract IEnumerable<PlatformDetectionType> GetPlatforms(T csElement);
        public abstract IEnumerable<MemberDeclarationSyntax> GenerateCode(T csElement, PlatformDetectionType platform);
    }
}