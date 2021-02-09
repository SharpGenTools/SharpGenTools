using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SharpGen.Generator
{
    public interface IMultiCodeGenerator<in TCsElement, out TSyntax>
        where TSyntax: SyntaxNode
    {
        IEnumerable<TSyntax> GenerateCode(TCsElement csElement);
    }
}
