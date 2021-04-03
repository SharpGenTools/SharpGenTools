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

using System.Collections.Generic;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGen.Model
{
    public abstract class CsMarshalBase : CsBase
    {
        /// <summary>
        ///   Public type used for element.
        /// </summary>
        public CsTypeBase PublicType { get; set; }

        /// <summary>
        ///   Internal type used for marshalling to native.
        /// </summary>
        public CsTypeBase MarshalType { get; set; }

        public bool HasPointer { get; protected internal set; }
        public bool IsArray { get; set; }
        public int ArrayDimensionValue { get; set; }
        public bool IsWideChar { get; set; }

        public IList<MarshallableRelation> Relations { get; set; }

        public bool IsBoolToInt => MarshalType is CsFundamentalType {IsIntegerType: true}
                                && PublicType == TypeRegistry.Boolean;

        public virtual bool IsOptional => false;
        public virtual bool IsRefIn => false;
        public virtual bool IsFastOut => false;

        public int Size => MarshalType.Size * (ArrayDimensionValue > 1 ? ArrayDimensionValue : 1);

        public bool IsValueType =>
            PublicType is CsStruct {GenerateAsClass: false} or CsEnum or CsFundamentalType {IsValueType: true};

        public bool PassedByNullableInstance => IsRefIn && IsValueType && !IsArray && IsOptional;
        public bool IsInterface => PublicType is CsInterface;
        public bool IsStructClass => PublicType is CsStruct {GenerateAsClass: true};
        public bool IsPrimitive => PublicType is CsFundamentalType {IsPrimitive: true};
        public bool IsString => PublicType is CsFundamentalType {IsString: true};
        public bool HasNativeValueType => PublicType is CsStruct {HasMarshalType: true};
        public bool IsStaticMarshal => PublicType is CsStruct {IsStaticMarshal: true};
        public bool IsInterfaceArray => PublicType is CsInterfaceArray;
        public bool IsNullableStruct => PassedByNullableInstance && !IsStructClass;
        public string IntermediateMarshalName => Name[0] == '@' ? $"_{Name.Substring(1)}" : $"_{Name}";

        public bool MappedToDifferentPublicType =>
            MarshalType != PublicType
            && !IsBoolToInt
            && !(MarshalType is CsFundamentalType {IsPointer: true} && HasPointer)
            && !(IsInterface && HasPointer);

        protected CsMarshalBase(CppElement cppElement, string name) : base(cppElement, name)
        {
            if (cppElement is CppMarshallable cppMarshallable)
            {
                IsArray = cppMarshallable.IsArray;
                HasPointer = cppMarshallable.HasPointer;

                ArrayDimensionValue = ParseArrayDimensionValue(cppMarshallable.ArrayDimension);

                // If array Dimension is 0, then it is not an array
                if (ArrayDimensionValue == 0)
                    IsArray = false;
            }

            int ParseArrayDimensionValue(string arrayDimension)
            {
                // TODO: handle multidimensional arrays
                if (!IsArray)
                    return 0;

                if (string.IsNullOrEmpty(arrayDimension))
                    return 0;

                return int.TryParse(arrayDimension, out var arrayDimensionValue)
                           ? arrayDimensionValue
                           : 1;
            }
        }

        public override IEnumerable<CsBase> AdditionalItems => AppendNonNull(
            base.AdditionalItems, PublicType, MarshalType
        );
    }
}