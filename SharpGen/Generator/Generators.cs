using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    static class Generators
    {
        public static readonly ICodeGenerator<CsVariable, MemberDeclarationSyntax> Constant = new ConstantCodeGenerator();

        public static readonly ICodeGenerator<CsProperty, MemberDeclarationSyntax> Property = new PropertyCodeGenerator();
    }
}
