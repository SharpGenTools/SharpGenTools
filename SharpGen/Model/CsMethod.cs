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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Generator;
using SharpGen.Transform;

namespace SharpGen.Model
{
    [DataContract]
    public class CsMethod : CsBase
    {
        public override CppElement CppElement
        {
            get => base.CppElement;
            set
            {
                base.CppElement = value;
                CppSignature = CppElement.ToString();
                ShortName = CppElement.ToShortString();
                CallingConvention = GetCallingConvention((CppMethod)value);
            }
        }

        protected virtual int MaxSizeReturnParameter
        {
            get { return 4; }
        }

        public CsMethod()
        {

        }

        public CsMethod(CppMethod cppMethod)
        {
            CppElement = cppMethod;
        }

        private List<CsParameter> _parameters;
        public List<CsParameter> Parameters
        {
            get { return _parameters ?? (_parameters = Items.OfType<CsParameter>().ToList()); }
        }

        public IEnumerable<CsParameter> PublicParameters
        {
            get
            {
                return Items.OfType<CsParameter>().Where(param => !param.IsUsedAsReturnType);
            }
        }

        [DataMember]
        public bool Hidden { get; set; }

        [DataMember]
        public string CallingConvention { get; set; }

        private static string GetCallingConvention(CppMethod method)
        {
            switch (method.CallingConvention)
            {
                case CppCallingConvention.StdCall:
                    return "StdCall";
                case CppCallingConvention.CDecl:
                    return "Cdecl";
                case CppCallingConvention.ThisCall:
                    return "ThisCall";
                default:
                    return "WinApi";
            }
        }

        public override void FillDocItems(IList<string> docItems, IDocumentationLinker manager)
        {
            foreach (var param in PublicParameters)
                docItems.Add("<param name=\"" + param.Name + "\">" + manager.GetSingleDoc(param) + "</param>");

            if (HasReturnType)
                docItems.Add("<returns>" + GetReturnTypeDoc(manager) + "</returns>");
        }

        public bool IsReturnStructLarge
        {
            get
            {
                if (ReturnType.PublicType is CsStruct csStruct)
                {
                    if (ReturnType.MarshalType.Type == typeof(IntPtr))
                        return false;

                    return csStruct.SizeOf > MaxSizeReturnParameter;
                }
                return false;
            }
        }
        
        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);

            AllowProperty = !tag.Property.HasValue || tag.Property.Value;

            IsPersistent = tag.Persist.HasValue && tag.Persist.Value;

            if(tag.CustomVtbl.HasValue)
                CustomVtbl = tag.CustomVtbl.Value;

            if (tag.MethodCheckReturnType.HasValue)
                CheckReturnType = tag.MethodCheckReturnType.Value;

            if (tag.ParameterUsedAsReturnType.HasValue)
                ForceReturnType = tag.ParameterUsedAsReturnType.Value;

            if (tag.AlwaysReturnHResult.HasValue) 
                AlwaysReturnHResult = tag.AlwaysReturnHResult.Value;

            if(tag.RawPtr.HasValue)
                RequestRawPtr = tag.RawPtr.Value;
        }

        [DataMember]
        public bool AllowProperty { get; set; }

        [DataMember]
        public bool CustomVtbl { get; set; }

        [DataMember]
        public bool IsPersistent { get; set; }

        [DataMember]
        public bool RequestRawPtr { get; set; }

        [DataMember]
        public int Offset { get; set; }

        [DataMember]
        public InteropMethodSignature Interop { get; set; }

        private string _cppSignature;

        [DataMember]
        public string CppSignature
        {
            get
            {
                return _cppSignature ?? "Unknown";
            }
            set => _cppSignature = value;
        }

        public override string DocUnmanagedName
        {
            get { return CppSignature; }
        }

        [DataMember]
        public string ShortName { get; set; }

        public override string DocUnmanagedShortName
        {
            get => ShortName;
        }

        [DataMember]
        public bool CheckReturnType { get; set; }

        [DataMember]
        public bool ForceReturnType { get; set; }

        [DataMember]
        public bool HideReturnType { get; set; }

        [DataMember]
        public bool AlwaysReturnHResult { get; set; }

        public bool HasReturnType
        {
            get { return !(ReturnType.PublicType.Type != null && ReturnType.PublicType.Type == typeof (void)); }
        }

        public bool HasPublicReturnType
        {
            get
            {
                foreach (var param in Parameters)
                {
                    if (param.IsUsedAsReturnType)
                        return true;
                }

                return HasReturnType;                
            }
        }

        [DataMember]
        public CsMarshalBase ReturnType { get; set; }


        /// <summary>
        /// Return the Public return type. If a out parameter is used as a public return type
        /// then use the type of the out parameter for the public API.
        /// </summary>
        public bool HasReturnTypeParameter
        {
            get
            {
                return Parameters.Any(param => param.IsUsedAsReturnType);
            }
        }

        /// <summary>
        /// Return the Public return type. If a out parameter is used as a public return type
        /// then use the type of the out parameter for the public API.
        /// </summary>
        public string PublicReturnTypeQualifiedName
        {
            get
            {
                foreach (var param in Parameters)
                {
                    if (param.IsUsedAsReturnType)
                        return param.PublicType.QualifiedName;
                }

                if (HideReturnType && !ForceReturnType)
                    return "void";

                return ReturnType.PublicType.QualifiedName;
            }
        }

        /// <summary>
        /// Returns the documentation for the return type
        /// </summary>
        public string GetReturnTypeDoc(IDocumentationLinker manager)
        {
            foreach (var param in Parameters)
            {
                if (param.IsUsedAsReturnType)
                {
                    return manager.GetSingleDoc(param);
                }
            }
            return manager.GetSingleDoc(ReturnType);
        }

        /// <summary>
        /// Return the name of the variable used to return the value
        /// </summary>
        public string ReturnName
        {
            get
            {
                foreach (var param in Parameters)
                {
                    if (param.IsUsedAsReturnType)
                        return param.Name;
                }
                return "__result__";
            }
        }

        public override object Clone()
        {
            var method = (CsMethod)base.Clone();

            // Clear cached parameters
            method._parameters = null;
            method.ResetItems();
            foreach (var parameter in Parameters)
                method.Add((CsParameter) parameter.Clone());
            method.Parent = null;
            return method;
        }
    }
}