using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract(Name = "Return")]
    public class CsReturnValue : CsMarshalCallableBase
    {
        public CsReturnValue()
        {
            Name = "__result__";
        }

        public string MarshalStorageLocation => "__result__native";

        public override bool IsOut => true;

        public override bool UsedAsReturn => true;

        public override bool PassedByNativeReference => true;
    }
}
