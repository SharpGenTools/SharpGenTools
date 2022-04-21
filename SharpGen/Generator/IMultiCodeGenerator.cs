using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SharpGen.Model;

namespace SharpGen.Generator;

public interface IMultiCodeGenerator<in TCsElement, out TSyntax>
    where TCsElement : CsBase where TSyntax : SyntaxNode
{
    IEnumerable<TSyntax> GenerateCode(TCsElement csElement);
}