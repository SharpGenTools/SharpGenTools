using Microsoft.CodeAnalysis;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    interface ICodeGenerator<in TCsElement, out TSyntax>
        where TCsElement : CsBase
        where TSyntax : SyntaxNode
    {
        TSyntax GenerateCode(TCsElement csElement);
    }
}
