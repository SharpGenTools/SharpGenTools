using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    public interface ITransformPreparer<in TCppElement, out TCsElement>
        where TCppElement: CppElement
        where TCsElement : CsBase
    {
        TCsElement Prepare(TCppElement cppElement);
    }
}
