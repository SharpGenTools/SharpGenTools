using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    public interface IMultiCodeGenerator<in TCsElement, out TSyntax>
        where TSyntax: SyntaxNode
    {
        IEnumerable<TSyntax> GenerateCode(TCsElement csElement);
    }
}
