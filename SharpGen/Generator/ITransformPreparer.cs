using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    interface ITransformPreparer
    {
        CsBase Prepare(CppElement cppElement);
    }

    interface ITransformPreparer<TCppElement> : ITransformPreparer
        where TCppElement: CppElement
    {
        CsBase Prepare(TCppElement cppElement);
    }
}
