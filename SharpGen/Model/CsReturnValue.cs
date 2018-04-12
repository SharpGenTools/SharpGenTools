using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract(Name = "Return")]
    public class CsReturnValue : CsMarshalBase
    {
        public CsReturnValue()
        {
            Name = "__result__";
        }

        public string MarshalStorageLocation => "__result__native";
    }
}
