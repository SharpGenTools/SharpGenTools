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
        CsMarshalBase RelatedMarshallable { get; set; }
    }

    [DataContract(Name = "Struct-Size")]
    public sealed class StructSizeRelation : MarshallableRelation
    {
    }

    [DataContract(Name = "Array-Length")]
    public sealed class ArrayLengthRelation : MarshallableRelation, IHasRelatedMarshallable
    {
        public CsMarshalBase RelatedMarshallable { get; set; }
    }

    [DataContract(Name = "Constant-Value")]
    public sealed class ConstantValueRelation : MarshallableRelation
    {
        public string Value { get; set; }
    }
}
