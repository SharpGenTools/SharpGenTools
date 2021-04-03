using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpGen.Transform;

namespace SharpGen.Model
{
    public sealed class CsFundamentalType : CsTypeBase
    {
        private static readonly Type VoidType = typeof(void);

        internal CsFundamentalType(Type type, PrimitiveTypeIdentity identity, string name) : this(type, name)
        {
            PrimitiveTypeIdentity = identity;
        }

        internal CsFundamentalType(Type type, string name) : base(null, name)
        {
            var typeInfo = type.GetTypeInfo();
            IsPrimitive = typeInfo.IsPrimitive;
            IsValueType = typeInfo.IsValueType || IsPrimitive;
            IsString = type == typeof(string);
            IsIntPtr = type == typeof(IntPtr) || type == typeof(UIntPtr);

            if (IsIntPtr)
            {
                IsUntypedPointer = true;
            }
            else if (typeInfo.IsPointer)
            {
                var reducedToPointer = ReducePointers(type, typeInfo);
                IsUntypedPointer = reducedToPointer == VoidType;
                IsTypedPointer = !IsUntypedPointer;
            }

            IsIntegerType = type == typeof(int)
                         || type == typeof(short)
                         || type == typeof(byte)
                         || type == typeof(long)
                         || type == typeof(uint)
                         || type == typeof(ushort)
                         || type == typeof(sbyte)
                         || type == typeof(ulong);

            // We need to ensure that we always return 8 (64-bit) even when running the generator on x64.
            Size = IsPointerSize ? 8 : SizeOf(type);
            Alignment = IsPointerSize ? -1 : GetAlignment(type);
        }

        internal readonly PrimitiveTypeIdentity? PrimitiveTypeIdentity;
        public override int Size { get; }
        private int? Alignment { get; }
        public bool IsValueType { get; }
        public bool IsPrimitive { get; }
        public bool IsIntPtr { get; }
        public bool IsPointer => IsTypedPointer || IsUntypedPointer;
        public bool IsUntypedPointer { get; }
        public bool IsTypedPointer { get; }
        public bool IsString { get; }
        public bool IsIntegerType { get; }
        public bool IsPointerSize => IsPointer || IsString;

        private static int SizeOf(Type type)
        {
            try
            {
                return Marshal.SizeOf(type);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates the natural alignment of a type. -1 if it is a pointer alignment (4 on x86, 8 on x64)
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int CalculateAlignment()
        {
            if (Alignment is { } alignment)
                return alignment;

            return base.CalculateAlignment();
        }

        private static int? GetAlignment(Type type)
        {
            if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
                return 8;

            if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                return 4;

            if (type == typeof(short) || type == typeof(ushort) || type == typeof(char))
                return 2;

            if (type == typeof(byte) || type == typeof(sbyte))
                return 1;

            return null;
        }

        private static Type ReducePointers(Type type, TypeInfo typeInfo)
        {
            while (true)
            {
                if (!typeInfo.IsPointer)
                    return type;

                type = typeInfo.GetElementType();
                typeInfo = type.GetTypeInfo();
            }
        }
    }
}
