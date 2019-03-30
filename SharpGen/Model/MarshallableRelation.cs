using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract(Name = "Marshallable-Relation")]
    public abstract class MarshallableRelation
    {
    }

    internal interface IHasRelatedMarshallable
    {
        string RelatedMarshallableName { get; set; }
    }

    [DataContract(Name = "Struct-Size")]
    public sealed class StructSizeRelation : MarshallableRelation
    {
    }

    [DataContract(Name = "Length")]
    public sealed class LengthRelation : MarshallableRelation, IHasRelatedMarshallable
    {
        [DataMember]
        public string RelatedMarshallableName { get; set; }
    }

    [DataContract(Name = "Constant-Value")]
    public sealed class ConstantValueRelation : MarshallableRelation
    {
        [DataMember]
        public string Value { get; set; }
    }
}
