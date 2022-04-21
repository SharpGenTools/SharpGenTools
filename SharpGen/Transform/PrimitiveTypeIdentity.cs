using System;

namespace SharpGen.Transform;

public readonly struct PrimitiveTypeIdentity : IEquatable<PrimitiveTypeIdentity>
{
    public readonly PrimitiveTypeCode Type;
    public readonly byte PointerCount;

    public PrimitiveTypeIdentity(PrimitiveTypeCode type, byte pointerCount = 0)
    {
        Type = type;
        PointerCount = pointerCount;
    }

    public bool Equals(PrimitiveTypeIdentity other) => Type == other.Type && PointerCount == other.PointerCount;

    public override bool Equals(object obj) => obj is PrimitiveTypeIdentity other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int) Type * 397) ^ PointerCount.GetHashCode();
        }
    }

    public static bool operator ==(PrimitiveTypeIdentity left, PrimitiveTypeIdentity right) => left.Equals(right);

    public static bool operator !=(PrimitiveTypeIdentity left, PrimitiveTypeIdentity right) => !left.Equals(right);
}