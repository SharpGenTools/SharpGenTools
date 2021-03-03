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
using System.Reflection;
using System.Xml.Serialization;

namespace SharpGen.Model
{
    /// <summary>
    /// Type used for template
    /// </summary>
    public sealed class InteropType : IEquatable<InteropType>
    {
        private string typeName;

        [ExcludeFromCodeCoverage(Reason = "Required for XML serialization.")]
        public InteropType()
        {
        }

        public InteropType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));
            TypeName = typeName;
        }

        public static implicit operator InteropType(Type input) => new(TransformTypeName(input));

        public static implicit operator InteropType(string input) => new(input);

        public string TypeName
        {
            get => typeName;
            set
            {
                if (!string.IsNullOrEmpty(typeName))
                    throw new InvalidOperationException($"{nameof(InteropType)} is immutable once assigned");

                typeName = value;
            }
        }

        private static string TransformTypeName(Type type)
        {
            if (type == typeof(int))
                return "int";
            if (type == typeof(short))
                return "short";
            if (type == typeof(void))
                return "void";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(long))
                return "long";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(ushort))
                return "ushort";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(sbyte))
                return "sbyte";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(char))
                return "char";
            if (type == typeof(decimal))
                return "decimal";

            if (type.IsPointer)
            {
                var elementType = type.GetElementType();
                if (elementType is not null)
                    if (elementType.IsPrimitive || elementType.IsPointer || elementType == typeof(void))
                        return $"{TransformTypeName(elementType)}*";
            }

            return type.FullName;
        }

        public bool Equals(InteropType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return typeName == other.typeName;
        }

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || obj is InteropType other && Equals(other);

        public override int GetHashCode() => typeName != null ? typeName.GetHashCode() : 0;

        public static bool operator ==(InteropType left, InteropType right) => Equals(left, right);

        public static bool operator !=(InteropType left, InteropType right) => !Equals(left, right);
    }
}