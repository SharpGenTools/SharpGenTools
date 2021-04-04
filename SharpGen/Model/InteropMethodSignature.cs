// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    [Flags]
    public enum InteropMethodSignatureFlags
    {
        None = 0x0,
        ForcedReturnBufferSig = 0x1,
        IsFunction = 0x2,
        CastToNativeLong = 0x4,
        CastToNativeULong = 0x8,
    }

    public sealed class InteropMethodSignature : IEquatable<InteropMethodSignature>
    {
        private const InteropMethodSignatureFlags FlagsToIgnoreForName =
            InteropMethodSignatureFlags.CastToNativeLong |
            InteropMethodSignatureFlags.CastToNativeULong |
            InteropMethodSignatureFlags.IsFunction;

        public InteropType ReturnType { get; set; }
        public List<InteropMethodSignatureParameter> ParameterTypes { get; } = new();

        public bool ForcedReturnBufferSig
        {
            get => Flags.HasFlag(InteropMethodSignatureFlags.ForcedReturnBufferSig);
            set
            {
                if (value)
                    Flags |= InteropMethodSignatureFlags.ForcedReturnBufferSig;
                else
                    Flags &= ~InteropMethodSignatureFlags.ForcedReturnBufferSig;
            }
        }

        public bool IsFunction
        {
            get => Flags.HasFlag(InteropMethodSignatureFlags.IsFunction);
            set
            {
                if (value)
                    Flags |= InteropMethodSignatureFlags.IsFunction;
                else
                    Flags &= ~InteropMethodSignatureFlags.IsFunction;
            }
        }

        public bool CastToNativeLong
        {
            get => Flags.HasFlag(InteropMethodSignatureFlags.CastToNativeLong);
            set
            {
                if (value)
                    Flags |= InteropMethodSignatureFlags.CastToNativeLong;
                else
                    Flags &= ~InteropMethodSignatureFlags.CastToNativeLong;
            }
        }

        public bool CastToNativeULong
        {
            get => Flags.HasFlag(InteropMethodSignatureFlags.CastToNativeULong);
            set
            {
                if (value)
                    Flags |= InteropMethodSignatureFlags.CastToNativeULong;
                else
                    Flags &= ~InteropMethodSignatureFlags.CastToNativeULong;
            }
        }

        public CppCallingConvention CallingConvention { get; set; }
        public InteropMethodSignatureFlags Flags { get; set; }

        private InteropMethodSignatureFlags FlagsForName => Flags & ~FlagsToIgnoreForName;

        public string Name
        {
            get
            {
                var returnTypeName = ReturnType.TypeName;
                returnTypeName = returnTypeName.Replace("*", "Ptr");
                returnTypeName = returnTypeName.Replace(".", string.Empty);
                return $"Call{CallingConvention}{(IsFunction ? "Func" : "")}{returnTypeName}_{FlagsForName}";
            }
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(ReturnType.TypeName);
            builder.Append(" Call");
            builder.Append(ReturnType.TypeName);
            builder.Append('(');
            for (int i = 0; i < ParameterTypes.Count; i++)
            {
                builder.Append(ParameterTypes[i].InteropType.TypeName);
                if ((i + 1) < ParameterTypes.Count)
                    builder.Append(',');
            }

            builder.Append(')');
            return builder.ToString();
        }

        public bool Equals(InteropMethodSignature other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!Equals(ReturnType, other.ReturnType)) return false;
            if (CallingConvention != other.CallingConvention) return false;
            if (Flags != other.Flags) return false;
            if (ParameterTypes.Count != other.ParameterTypes.Count) return false;

            var typeComparer = InteropMethodSignatureParameter.TypeComparer;

            for (var i = 0; i < ParameterTypes.Count; i++)
            {
                if (!typeComparer.Equals(ParameterTypes[i], other.ParameterTypes[i])) return false;
            }

            return true;
        }

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || obj is InteropMethodSignature other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ReturnType != null ? ReturnType.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ CallingConvention.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Flags;
                hashCode = (hashCode * 397) ^ ParameterTypes.Count;

                var typeComparer = InteropMethodSignatureParameter.TypeComparer;

                foreach (var parameter in ParameterTypes)
                {
                    hashCode = (hashCode * 397) ^ typeComparer.GetHashCode(parameter);
                }

                return hashCode;
            }
        }

        public static bool operator ==(InteropMethodSignature left, InteropMethodSignature right) =>
            Equals(left, right);

        public static bool operator !=(InteropMethodSignature left, InteropMethodSignature right) =>
            !Equals(left, right);
    }
}