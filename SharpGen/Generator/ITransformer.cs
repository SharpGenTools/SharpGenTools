using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{

    interface ITransformer
    {
        void Process(CsBase csElement);
    }

    interface ITransformer<TCsElement> : ITransformer
        where TCsElement: CsBase
    {
        void Process(TCsElement csElement);
    }
}
