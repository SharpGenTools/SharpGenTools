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
using System.Linq;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    /// <summary>
    ///   A structElement that maps to a native struct
    /// </summary>
    public sealed class CsStruct : CsTypeBase
    {
        private CsBaseItemListCache<CsField> _fields;
        private bool hasMarshalType, hasMarshalTypeFromFields;

        public CsStruct(CppStruct cppStruct, string name) : base(cppStruct, name)
        {
            if (cppStruct == null)
                return;

            var tag = cppStruct.Rule;

            Align = tag.StructPack ?? Align;
            HasMarshalType = tag.StructHasNativeValueType ?? HasMarshalType;
            GenerateAsClass = tag.StructToClass ?? GenerateAsClass;
            HasCustomMarshal = tag.StructCustomMarshal ?? HasCustomMarshal;
            IsStaticMarshal = tag.IsStaticMarshal ?? IsStaticMarshal;
            HasCustomNew = tag.StructCustomNew ?? HasCustomNew;
        }

        public override uint Size => StructSize;

        public uint StructSize { private get; set; }

        /// <summary>
        ///   Packing alignment for this type (Default is 0 => Platform default)
        /// </summary>
        public int Align { get; set; }

        public IReadOnlyList<CsField> Fields => _fields.GetList(this);

        public IEnumerable<CsField> PublicFields => _fields.Enumerate(this)
                                                           .Where(field => field.Relations.Count == 0);

        /// <summary>
        /// Gets the variables stored in this container.
        /// </summary>
        /// <value>The variables.</value>
        public IEnumerable<CsVariable> Variables => Items.OfType<CsVariable>();

        /// <summary>
        ///   True if this structure is using an explicit layout else it's a sequential structure
        /// </summary>
        public bool ExplicitLayout { get; set; }

        /// <summary>
        ///   True if this struct needs an internal marshal type
        /// </summary>
        public bool HasMarshalType
        {
            get => hasMarshalType || hasMarshalTypeFromFields
                                  || HasCustomMarshal || IsStaticMarshal || HasCustomNew || GenerateAsClass;
            set => hasMarshalType = value;
        }

        public bool HasCustomMarshal { get; }
        public bool IsStaticMarshal { get; set; }
        public bool GenerateAsClass { get; }
        public bool HasCustomNew { get; set; }

        /// <summary>
        /// True if the native type this structure represents is a native primitive type
        /// </summary>
        public bool IsNativePrimitive { get; set; }

        /// <summary>
        ///   List of declared inner structs
        /// </summary>
        public IEnumerable<CsStruct> InnerStructs => Items.OfType<CsStruct>();

        protected override uint? AlignmentCore
        {
            get
            {
                // See llvm/lib/IR/DataLayout.cpp:[StructLayout::StructLayout]
                // See llvm/include/llvm/Support/Alignment.h [Align::Align]
                uint structAlignment = 1;

                foreach (var field in Fields)
                {
                    var fieldAlignment = field.MarshalType.Alignment;

                    if (fieldAlignment is not { } value)
                        return null;

                    structAlignment = Math.Max(structAlignment, value);
                }

                return structAlignment;
            }
        }

        public bool IsFullyMapped { get; set; } = true;

        private protected override IEnumerable<IExpiring> ExpiringOnItemsChange
        {
            get
            {
                yield return _fields.Expiring;
            }
        }

        protected override void OnItemsChanged()
        {
            // ReSharper disable once LocalVariableHidesMember
            var hasMarshalType = false;

            foreach (var field in Fields)
            {
                if (field.Relations.Count != 0)
                    hasMarshalType = true;

                var fieldHasMarshalType = field.PublicType != field.MarshalType
                                       || field.HasNativeValueType
                                       || field.IsArray;

                // BoolToInt doesn't generate native Marshaling although they have a different marshaller
                if (fieldHasMarshalType && (!field.IsBoolToInt || field.IsArray))
                    hasMarshalType = true;
            }

            hasMarshalTypeFromFields = hasMarshalType;
        }
    }
}