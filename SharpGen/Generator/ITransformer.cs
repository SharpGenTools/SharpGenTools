using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    public interface ITransformer<TCsElement>
        where TCsElement: CsBase
    {
        void Process(TCsElement csElement);
    }
}
