using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract]
    public abstract class CsMarshalCallableBase : CsMarshalBase
    {
        public virtual bool UsedAsReturn => false;

        public virtual bool IsIn => false;

        public virtual bool IsRef => false;

        public virtual bool IsOut => false;

        public virtual bool IsFixed => false;

        public virtual bool RefInPassedByValue => false;

        public virtual bool PassedByNativeReference => false;
    }
}
