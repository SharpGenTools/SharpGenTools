using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract(Name = "Builtin-Type")]
    public class CsFundamentalType : CsTypeBase
    {
        public CsFundamentalType(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// The built-in .NET type that this type instance represents.
        /// </summary>
        public Type Type { get; private set; }

        [DataMember]
        public string BuiltinTypeName
        {
            get => Type?.FullName;
            set
            {
                if (value != null)
                {
                    Type = Type.GetType(value);
                }
            }
        }

        private int? size;

        public override int Size => size ??= GetSize();

        private int GetSize()
        {
            try
            {
                var type = Type;
                if (!IsPointerType(type))
                {
                    return Marshal.SizeOf(type);
                }
                // We need to ensure that we always return 8 (64-bit) even when running the generator on x64.
                return 8;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public bool IsPointer => IsPointerType(Type);

        private static bool IsPointerType(Type type) =>
            type == typeof(IntPtr) || type == typeof(UIntPtr) || type == typeof(void*);

        /// <summary>
        /// Calculates the natural alignment of a type. -1 if it is a pointer alignment (4 on x86, 8 on x64)
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int CalculateAlignment()
        {
            var type = Type;

            if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
            {
                return 8;
            }

            if (type == typeof(int) || type == typeof(uint) ||
                type == typeof(float))
            {
                return 4;
            }

            if (type == typeof(short) || type == typeof(ushort) || type == typeof(char))
            {
                return 2;
            }

            if (type == typeof(byte) || type == typeof(sbyte))
            {
                return 1;
            }

            if (IsPointerType(type))
            {
                return -1;
            }

            return base.CalculateAlignment();
        }
    }
}
