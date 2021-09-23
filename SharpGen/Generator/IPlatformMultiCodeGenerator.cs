using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public interface IPlatformMultiCodeGenerator<in TCsElement, out TSyntax> : IPlatformCodeGenerator<TCsElement>
        where TCsElement : CsBase where TSyntax : SyntaxNode
    {
        IEnumerable<TSyntax> GenerateCode(TCsElement csElement, PlatformDetectionType platform);
    }
}