using Microsoft.CodeAnalysis;
using SharpGen.Model;

namespace SharpGen.Generator;

public interface ISingleCodeGenerator<in TCsElement, out TSyntax>
    where TCsElement : CsBase where TSyntax : SyntaxNode
{
    TSyntax GenerateCode(TCsElement csElement);
}