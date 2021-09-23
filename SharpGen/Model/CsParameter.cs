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

using System.Diagnostics;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsParameter : CsMarshalCallableBase
    {
        private bool isOptional, usedAsReturn;
        private const int SizeOfLimit = 16;

        public CsParameterAttribute Attribute { get; set; } = CsParameterAttribute.In;

        public string DefaultValue { get; }
        private bool ForcePassByValue { get; }
        public bool HasParams { get; }
        public bool IsFast { get; }

        public override bool IsArray
        {
            get => base.IsArray && !IsString;
            set => base.IsArray = value;
        }

        public override bool HasPointer => base.HasPointer || ArraySpecification.HasValue;

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

        public bool IsOptional
        {
            // Arrays of reference types (interfaces) support null values
            get => isOptional || PublicType is CsInterface && !IsOut && IsArray;
            private set => isOptional = value;
        }

        /// <summary>
        /// Parameter is an Out parameter and passed by pointer.
        /// </summary>
        public override bool IsOut => Attribute == CsParameterAttribute.Out;

        /// <summary>
        /// Parameter is an In/Out parameter and passed by pointer.
        /// </summary>
        public bool IsRef => Attribute == CsParameterAttribute.Ref;

        /// <summary>
        /// Parameter is an In parameter and passed by pointer.
        /// </summary>
        public bool IsRefIn => Attribute == CsParameterAttribute.RefIn;

        private bool RefInPassedByValue => IsRefIn && IsValueType && !IsArray
                                        && (PublicType.Size <= SizeOfLimit && !HasNativeValueType || ForcePassByValue);

        public bool PassedByManagedReference => (IsRef || IsRefIn)
                                             && !(PassedByNullableInstance || RefInPassedByValue) && !IsStructClass;

        public bool PassedByNullableInstance => IsRefIn && IsValueType && !IsArray && IsOptional;
        public bool IsNullableStruct => PassedByNullableInstance && !IsStructClass;
        public bool IsNullable => IsOptional && (IsArray || IsInterface || IsNullableStruct || IsStructClass);
        public override bool PassedByNativeReference => !IsIn;

        public override bool IsLocalManagedReference =>
            Attribute is CsParameterAttribute.Ref or CsParameterAttribute.Out;

        public override bool UsedAsReturn => usedAsReturn;

        internal void MarkUsedAsReturn() => usedAsReturn = true;

        public CsParameter Clone()
        {
            var parameter = (CsParameter) MemberwiseClone();
            parameter.ResetParentAfterClone();
            return parameter;
        }

        public CsParameter(Ioc ioc, CppParameter cppParameter, string name) : base(ioc, cppParameter, name)
        {
            if (cppParameter == null)
                return;

            var paramAttribute = cppParameter.Attribute;
            var paramRule = cppParameter.Rule;

            usedAsReturn = paramRule.ParameterUsedAsReturnType ?? UsedAsReturn;
            DefaultValue = paramRule.DefaultValue;

            if (HasFlag(paramAttribute, ParamAttribute.Buffer))
                IsArray = true;

            if (HasFlag(paramAttribute, ParamAttribute.Fast))
                IsFast = true;

            if (HasFlag(paramAttribute, ParamAttribute.Value))
                ForcePassByValue = true;

            if (HasFlag(paramAttribute, ParamAttribute.Params))
                HasParams = true;

            if (HasFlag(paramAttribute, ParamAttribute.Optional))
                IsOptional = true;

            static bool HasFlag(ParamAttribute value, ParamAttribute flag) => (value & flag) == flag;
        }
    }
}