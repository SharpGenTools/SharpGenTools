using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime.Win32
{
    /// <summary>
    /// Type of a variant
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct VariantFullType : IEquatable<VariantFullType>
    {
        private readonly ushort vt;

        public VariantElementType ElementType => (VariantElementType) (vt & 0x0fff);
        public VariantType Type => (VariantType) (vt & 0xf000);

        public VariantFullType(short value) => vt = unchecked((ushort) value);
        public VariantFullType(ushort value) => vt = value;

        public static implicit operator short(VariantFullType value) => unchecked((short) value.vt);
        public static implicit operator ushort(VariantFullType value) => value.vt;
        public static implicit operator VariantFullType(short value) => new(value);
        public static implicit operator VariantFullType(ushort value) => new(value);

        public VariantFullType(VariantElementType elementType, VariantType type)
        {
            vt = (ushort)((ushort) type | (ushort) elementType);
        }

        public bool Equals(VariantFullType other) => vt == other.vt;
        public override bool Equals(object obj) => obj is VariantFullType other && Equals(other);
        public override int GetHashCode() => vt.GetHashCode();
        public static bool operator ==(VariantFullType left, VariantFullType right) => left.Equals(right);
        public static bool operator !=(VariantFullType left, VariantFullType right) => !left.Equals(right);

        public override string ToString() => $"{ElementType} {Type}";
    }
}