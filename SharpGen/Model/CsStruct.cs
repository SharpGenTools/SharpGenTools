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
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    /// <summary>
    ///   A structElement that maps to a native struct
    /// </summary>
    [DataContract(Name = "Struct")]
    public class CsStruct : CsTypeBase
    {
        public CsStruct()
            : this(null)
        {
        }

        public CsStruct(CppStruct cppStruct) 
        {
            CppElement = cppStruct;
        }

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);
            Align = tag.StructPack ?? 0;
            HasMarshalType = tag.StructHasNativeValueType ?? false;
            GenerateAsClass = tag.StructToClass ?? false;
            HasCustomMarshal = tag.StructCustomMarshal ?? false;
            IsStaticMarshal = tag.IsStaticMarshal ?? false;
            HasCustomNew = tag.StructCustomNew ?? false;

            if (HasCustomMarshal || IsStaticMarshal || HasCustomNew || GenerateAsClass)
            {
                HasMarshalType = true;
            }
        }

        public override int Size => _Size_;

        [DataMember(Name = "Size")]
        public int _Size_ { get; set; }
        
        /// <summary>
        ///   Packing alignment for this type (Default is 0 => Platform default)
        /// </summary>
        [DataMember]
        public int Align { get; set; }

        public void SetSize(int size)
        {
            _Size_ = size;
        }

        public IEnumerable<CsField> Fields
        {
            get { return Items.OfType<CsField>(); }
        }

        /// <summary>
        /// Gets the variables stored in this container.
        /// </summary>
        /// <value>The variables.</value>
        public IEnumerable<CsVariable> Variables
        {
            get { return Items.OfType<CsVariable>(); }
        }

        /// <summary>
        ///   True if this structure is using an explicit layout else it's a sequential structure
        /// </summary>
        [DataMember]
        public bool ExplicitLayout { get; set; }
        
        /// <summary>
        ///   True if this struct needs an internal marshal type
        /// </summary>
        [DataMember]
        public bool HasMarshalType { get; set; }

        [DataMember]
        public bool HasCustomMarshal { get; set; }

        [DataMember]
        public bool IsStaticMarshal { get; set; }

        [DataMember]
        public bool GenerateAsClass { get; set; }

        [DataMember]
        public bool HasCustomNew { get; set; }

        /// <summary>
        /// True if the native type this structure represents is a native primitive type
        /// </summary>
        [DataMember]
        public bool IsNativePrimitive { get; set; }

        /// <summary>
        ///   List of declared inner structs
        /// </summary>
        public IEnumerable<CsStruct> InnerStructs
        {
            get { return Items.OfType<CsStruct>(); }
        }

        public override int CalculateAlignment()
        {
            int structAlignment = 0;
            foreach(var field in Fields)
            {
                var fieldAlignment = (field.MarshalType ?? field.PublicType).CalculateAlignment();
                if(fieldAlignment < 0)
                {
                    structAlignment = fieldAlignment;
                    break;
                }
                if(fieldAlignment > structAlignment)
                {
                    structAlignment = fieldAlignment;
                }
            }

            return structAlignment;
        }
    }
}