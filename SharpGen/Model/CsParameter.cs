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
    public class CsParameter : CsMarshalCallableBase
    {
        private const int SizeOfLimit = 16;
        [DataMember]
        public CsParameterAttribute Attribute { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }

        [DataMember]
        public bool ForcePassByValue { get; set; }

        [DataMember]
        public bool HasParams { get; set; }

        [DataMember]
        public bool IsFast { get; set; }

        public override bool IsFastOut
        {
            get { return IsFast && IsOut; }
        }

        public override bool IsFixed
        {
            get
            {
                if (Attribute == CsParameterAttribute.Ref || Attribute == CsParameterAttribute.RefIn)
                {
                    return !(PassedByNullableInstance || RefInPassedByValue);
                }
                if (Attribute == CsParameterAttribute.Out && !IsBoolToInt)
                    return true;
                if (IsArray && !IsInterfaceArray)
                    return true;
                return false;
            }
        }

        public override bool IsIn
        {
            get { return Attribute == CsParameterAttribute.In; }
        }

        public bool IsInInterfaceArrayLike
        {
            get
            {
                return IsArray && PublicType is CsInterface iface && !iface.IsCallback && !IsOut;
            }
        }

        public override bool IsOptional => OptionalParameter;

        /// <summary>
        /// Parameter is an Out parameter and passed by pointer.
        /// </summary>
        public override bool IsOut
        {
            get { return Attribute == CsParameterAttribute.Out; }
        }

        /// <summary>
        /// Parameter is an In/Out parameter and passed by pointer.
        /// </summary>
        public override bool IsRef
        {
            get { return Attribute == CsParameterAttribute.Ref; }
        }

        /// <summary>
        /// Parameter is an In parameter and passed by pointer.
        /// </summary>
        public override bool IsRefIn
        {
            get { return Attribute == CsParameterAttribute.RefIn; }
        }

        public override bool RefInPassedByValue
        {
            get
            {
                return IsRefIn && IsValueType && !IsArray
                       && ((PublicType.Size <= SizeOfLimit && !HasNativeValueType) || ForcePassByValue);
            }
        }

        public bool PassedByManagedReference
            => (IsRef || IsRefIn)
                && (!(PassedByNullableInstance || RefInPassedByValue)
                && !IsStructClass);

        public override bool PassedByNativeReference => IsRefIn || IsRef || IsOut;

        [DataMember]
        public bool IsUsedAsReturnType { get; set; }

        public override bool UsedAsReturn => IsUsedAsReturnType;

        [DataMember]
        public bool OptionalParameter { get; set; }

        public override object Clone()
        {
            var parameter = (CsParameter)base.Clone();
            parameter.Parent = null;
            return parameter;
        }

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);
            if (tag.ParameterUsedAsReturnType.HasValue)
                IsUsedAsReturnType = tag.ParameterUsedAsReturnType.Value;
            if (tag.ParameterAttribute.HasValue)
            {
                if ((tag.ParameterAttribute.Value & ParamAttribute.Fast) != 0)
                {
                    IsFast = true;
                }

                if ((tag.ParameterAttribute.Value & ParamAttribute.Value) != 0)
                {
                    ForcePassByValue = true;
                }
            }

            DefaultValue = tag.DefaultValue;
        }
    }
}