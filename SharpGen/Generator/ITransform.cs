using Microsoft.CodeAnalysis;
using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    interface ITransform<TCsElement, TCppElement>
        where TCsElement: CsBase
        where TCppElement: CppElement
    {
        TCsElement Prepare(TCppElement cppElement);

        void Process(TCsElement csElement);

        SyntaxNode GenerateCodeForElement(TCsElement csElement);
    }
}
