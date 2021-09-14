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
using System.Collections.Immutable;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Generator.Marshallers;
using SharpGen.Transform;

namespace SharpGen.Model
{
    public abstract class CsMarshalBase : CsBase
    {
#nullable enable
        private IReadOnlyList<MarshallableRelation>? relations;
        private CsTypeBase? marshalType;
        private CsTypeBase publicType;

        /// <summary>
        ///   Public type used for element.
        /// </summary>
        public CsTypeBase PublicType
        {
            get => publicType;
            set => publicType = value ?? throw new ArgumentException("Public type cannot be null");
        }

        /// <summary>
        ///   Internal type used for marshalling to native.
        /// </summary>
        public CsTypeBase MarshalType
        {
            get => marshalType ?? PublicType;
            set => marshalType = value;
        }

        public void SetPublicResetMarshalType(CsTypeBase type)
        {
            PublicType = type;
            marshalType = null;
        }

        public virtual bool HasPointer { get; }

        public ArraySpecification? ArraySpecification { get; private set; }
        public bool IsWideChar { get; set; }

        public virtual bool IsArray
        {
            get => ArraySpecification.HasValue;
            set
            {
                switch (value)
                {
                    case true when ArraySpecification.HasValue:
                        return;
                    case true:
                        ArraySpecification = new ArraySpecification();
                        return;
                    case false:
                        ArraySpecification = null;
                        break;
                }
            }
        }

        public int ArrayDimensionValue => ArraySpecification is {Dimension: { } value} ? checked((int) value) : 0;
        public uint ArrayDimensionValueUnsigned => ArraySpecification is {Dimension: { } value} ? value : 0;

        public IReadOnlyList<MarshallableRelation> Relations
        {
            get => relations ?? ImmutableList<MarshallableRelation>.Empty;
            set => relations = value;
        }

        public bool IsBoolToInt => MarshalType is CsFundamentalType {IsIntegerType: true}
                                && PublicType == TypeRegistry.Boolean;

        public uint Size => MarshalType.Size * (ArraySpecification is {Dimension: { } value} ? Math.Max(value, 1) : 1);

        public bool IsValueType =>
            PublicType is CsStruct {GenerateAsClass: false} or CsEnum or CsFundamentalType {IsValueType: true};

        public bool IsInterface => PublicType is CsInterface;
        public bool IsStructClass => PublicType is CsStruct {GenerateAsClass: true};
        public bool IsPrimitive => PublicType is CsFundamentalType {IsPrimitive: true} or CsEnum;
        public bool IsString => PublicType is CsFundamentalType {IsString: true};
        public bool HasNativeValueType => PublicType is CsStruct {HasMarshalType: true};
        public bool IsStaticMarshal => PublicType is CsStruct {IsStaticMarshal: true};
        public bool IsInterfaceArray => PublicType is CsInterfaceArray;

        /// <remarks>
        /// Used in 2 cases:
        /// <list type="number">
        /// <item>
        /// <description>Backing field in structs for non-trivial cases</description>
        /// </item>
        /// <item>
        /// <description>Pinned Span element pointer from <see cref="ArrayMarshallerBase"/></description>
        /// </item>
        /// </list>
        /// </remarks>
        public string IntermediateMarshalName => Name[0] == '@' ? $"_{Name.Substring(1)}" : $"_{Name}";

        public bool MappedToDifferentPublicType =>
            MarshalType != PublicType
            && !IsBoolToInt
            && !(MarshalType is CsFundamentalType {IsPointer: true} && HasPointer)
            && !(IsInterface && HasPointer);

        public StringMarshalType StringMarshal { get; } = StringMarshalType.GlobalHeap;

#nullable restore

        protected CsMarshalBase(Ioc ioc, CppElement cppElement, string name) : base(cppElement, name)
        {
            if (cppElement is CppMarshallable cppMarshallable)
            {
                HasPointer = cppMarshallable.HasPointer;

                ArraySpecification = ParseArrayDimensionValue(cppMarshallable.IsArray, cppMarshallable.ArrayDimension);
            }

            if (cppElement is { Rule: { StringMarshal: { } stringMarshal } })
                StringMarshal = stringMarshal;

            ArraySpecification? ParseArrayDimensionValue(bool isArray, string arrayDimension)
            {
                if (!isArray || string.IsNullOrEmpty(arrayDimension))
                    return null;

                if (arrayDimension.Contains(","))
                {
                    ioc.Logger.Warning(null, "SharpGen might not handle multidimensional arrays properly.");
                }

                // TODO: handle multidimensional arrays
                return uint.TryParse(arrayDimension, out var arrayDimensionValue) && arrayDimensionValue >= 1
                           ? new ArraySpecification(arrayDimensionValue)
                           : null;
            }
        }

        public override IEnumerable<CsBase> AdditionalItems => AppendNonNull(
            base.AdditionalItems, PublicType, MarshalType
        );
    }
}