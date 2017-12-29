using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    public interface ITransformer<TCsElement>
        where TCsElement: CsBase
    {
        void Process(TCsElement csElement);
    }
}
