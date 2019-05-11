using Microsoft.CodeAnalysis;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    public interface ICodeGenerator<in TCsElement, out TSyntax>
        where TSyntax : SyntaxNode
    {
        TSyntax GenerateCode(TCsElement csElement);
    }
}
