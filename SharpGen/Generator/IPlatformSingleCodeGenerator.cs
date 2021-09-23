using Microsoft.CodeAnalysis;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public interface IPlatformSingleCodeGenerator<in TCsElement, out TSyntax> : IPlatformCodeGenerator<TCsElement>
        where TCsElement : CsBase where TSyntax : SyntaxNode
    {
        TSyntax GenerateCode(TCsElement csElement, PlatformDetectionType platform);
    }
}