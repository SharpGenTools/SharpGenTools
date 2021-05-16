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
using System.Text;

namespace SharpGen.CppModel
{
    /// <summary>
    /// Type declaration.
    /// </summary>
    public abstract class CppMarshallable : CppElement
    {
#nullable enable
        private string? typeName;
        private string? arrayDimension;

        public string TypeName
        {
            get => (Rule.OverrideNativeType == true ? Rule.MappingType : typeName)
                ?? throw new InvalidOperationException(
                       $"{nameof(CppMarshallable)} is expected to have {nameof(TypeName)}"
                   );
            set => typeName = value;
        }

        public string Pointer
        {
            get => Rule.Pointer ?? string.Empty;
            set => Rule.Pointer = value;
        }

        public bool Const { get; set; }
        public bool IsArray => ArrayDimension != null;

        public string? ArrayDimension
        {
            get => Rule.TypeArrayDimension switch
            {
                { } arrayDimensionValue => arrayDimensionValue,
                _ => arrayDimension
            };
            set => arrayDimension = value;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Const)
                builder.Append("const ");
            builder.Append(TypeName);
            builder.Append(Pointer);

            if (!string.IsNullOrEmpty(Name))
            {
                builder.Append(' ');
                builder.Append(Name);
            }

            if (IsArray)
            {
                builder.Append('[');
                builder.Append(ArrayDimension);
                builder.Append(']');
            }

            return builder.ToString();
        }

        public bool HasPointer
        {
            get
            {
                var pointer = Pointer;
                return !string.IsNullOrEmpty(pointer) && (pointer.Contains("*") || pointer.Contains("&"));
            }
        }
#nullable restore

        protected CppMarshallable(string name) : base(name)
        {
        }
    }
}