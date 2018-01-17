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
using SharpGen.Config;
using SharpGen.CppModel;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace SharpGen.Model
{
    [DataContract(Name = "Parameter")]
    public class CsParameter : CsMarshalBase
    {
        public CsParameter()
        {
        }

        public override CppElement CppElement
        {
            get => base.CppElement;
            set
            {
                base.CppElement = value;
                NativeParamAttribute = ((CppParameter)value).Attribute;
            }
        }
        
        [DataMember]
        public CsParameterAttribute Attribute { get; set; }

        [DataMember]
        public bool HasParams { get; set; }

        [DataMember]
        public bool IsOptional { get; set; }

        [DataMember]
        public bool IsUsedAsReturnType { get; set; }

        [DataMember]
        public bool IsFast { get; set; }

        public bool IsFastOut
        {
            get { return IsFast && IsOut; }
        }

        [DataMember]
        public string DefaultValue { get; set; }

        private const int SizeOfLimit = 16;

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);
            if (tag.ParameterUsedAsReturnType.HasValue)
                IsUsedAsReturnType = tag.ParameterUsedAsReturnType.Value;
            if (tag.ParameterAttribute.HasValue && (tag.ParameterAttribute.Value & ParamAttribute.Fast) != 0)
                IsFast = true;

            DefaultValue = tag.DefaultValue;
        }

        public bool IsFixed
        {
            get
            {
                if (Attribute == CsParameterAttribute.Ref || Attribute == CsParameterAttribute.RefIn)
                {
                    if (IsRefInValueTypeOptional || IsRefInValueTypeByValue)
                        return false;
                    return true;
                }
                if (Attribute == CsParameterAttribute.Out && !IsBoolToInt)
                    return true;
                if (IsArray && !IsComArray)
                    return true;
                return false;
            }
        }

        public string TempName
        {
            get { return Name + "_"; }
        }

        public bool IsRef
        {
            get { return Attribute == CsParameterAttribute.Ref; }
        }

        public bool IsComArray
        {
            get
            {
                return PublicType is CsComArray;
            }
        }

        public bool IsInComArrayLike
        {
            get
            {
                return IsArray && IsComObject && !IsOut;
            }
        }

        public bool IsComObject
        {
            get
            {
                return PublicType.GetType() == typeof(CsInterface);
            }
        }

        public bool IsRefIn
        {
            get { return Attribute == CsParameterAttribute.RefIn; }
        }

        public bool IsIn
        {
            get { return Attribute == CsParameterAttribute.In; }
        }

        public bool IsOut
        {
            get { return Attribute == CsParameterAttribute.Out; }
        }

        public bool IsPrimitive
        {
            get { return PublicType.Type != null && PublicType.Type.GetTypeInfo().IsPrimitive; }
        }

        public bool IsString
        {
            get { return PublicType.Type == typeof (string); }
        }

        public bool IsValueType
        {
            get { return PublicType is CsStruct || PublicType is CsEnum ||
                    (PublicType.Type != null && (PublicType.Type.GetTypeInfo().IsValueType || PublicType.Type.GetTypeInfo().IsPrimitive)); }
        }

        public bool IsStructClass
        {
            get { return PublicType is CsStruct csStruct && csStruct.GenerateAsClass; }
        }

        public bool HasNativeValueType
        {
            get { return (PublicType is CsStruct csStruct && csStruct.HasMarshalType) ; }
        }

        public bool IsStaticMarshal
        {
            get { return (PublicType is CsStruct csStruct && csStruct.IsStaticMarshal); }
        }

        public bool IsRefInValueTypeOptional
        {
            get { return IsRefIn && IsValueType && !IsArray && IsOptional; }
        }

        public ParamAttribute NativeParamAttribute { get; set; }

        public bool IsRefInValueTypeByValue
        {
            get
            {
                return IsRefIn && IsValueType && !IsArray
                       && ((PublicType.SizeOf <= SizeOfLimit && !HasNativeValueType) || (NativeParamAttribute & ParamAttribute.Value) != 0);
            }
        }

        public override object Clone()
        {
            var parameter = (CsParameter)base.Clone();
            parameter.Parent = null;
            return parameter;
        }
    }
}