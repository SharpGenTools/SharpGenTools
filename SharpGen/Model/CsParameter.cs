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

using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsParameter : CsMarshalCallableBase
    {
        private const int SizeOfLimit = 16;

        public CsParameterAttribute Attribute { get; set; }

        public string DefaultValue { get; }

        private bool ForcePassByValue { get; }

        public bool HasParams { get; }

        private bool IsFast { get; }

        public override bool IsFastOut => IsFast && IsOut;

        public override bool IsFixed
        {
            get
            {
                if (IsRef || IsRefIn)
                    return !(PassedByNullableInstance || RefInPassedByValue);
                if (IsOut && !IsBoolToInt)
                    return true;
                if (IsArray && !IsInterfaceArray)
                    return true;
                return false;
            }
        }

        public bool IsIn => Attribute == CsParameterAttribute.In;

        public bool IsInInterfaceArrayLike => IsArray && PublicType is CsInterface {IsCallback: false} && !IsOut;

        public override bool IsOptional => OptionalParameter;

        /// <summary>
        /// Parameter is an Out parameter and passed by pointer.
        /// </summary>
        public override bool IsOut => Attribute == CsParameterAttribute.Out;

        /// <summary>
        /// Parameter is an In/Out parameter and passed by pointer.
        /// </summary>
        public override bool IsRef => Attribute == CsParameterAttribute.Ref;

        /// <summary>
        /// Parameter is an In parameter and passed by pointer.
        /// </summary>
        public override bool IsRefIn => Attribute == CsParameterAttribute.RefIn;

        private bool RefInPassedByValue => IsRefIn && IsValueType && !IsArray
                                        && (PublicType.Size <= SizeOfLimit && !HasNativeValueType || ForcePassByValue);

        public bool PassedByManagedReference => (IsRef || IsRefIn)
                                             && !(PassedByNullableInstance || RefInPassedByValue) && !IsStructClass;

        public override bool PassedByNativeReference => IsRefIn || IsRef || IsOut;

        public bool IsUsedAsReturnType { get; }

        public override bool UsedAsReturn => IsUsedAsReturnType;

        public bool OptionalParameter { get; set; }

        public CsParameter Clone()
        {
            var parameter = (CsParameter) MemberwiseClone();
            parameter.ResetParentAfterClone();
            return parameter;
        }

        public CsParameter(CppParameter cppParameter, string name) : base(cppParameter, name)
        {
            if (cppParameter != null)
                HasParams = (cppParameter.Attribute & ParamAttribute.Params) == ParamAttribute.Params;

            var paramRule = cppParameter?.Rule;

            IsUsedAsReturnType = paramRule?.ParameterUsedAsReturnType ?? IsUsedAsReturnType;
            DefaultValue = paramRule?.DefaultValue ?? DefaultValue;

            if (paramRule?.ParameterAttribute is { } attribute)
            {
                if ((attribute & ParamAttribute.Fast) != 0)
                    IsFast = true;

                if ((attribute & ParamAttribute.Value) != 0)
                    ForcePassByValue = true;

                if ((attribute & ParamAttribute.Params) == ParamAttribute.Params)
                    HasParams = true;
            }
        }
    }
}