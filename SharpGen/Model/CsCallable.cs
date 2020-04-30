using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGen.Model
{
    [DataContract]
    public abstract class CsCallable : CsBase
    {
        public override CppElement CppElement
        {
            get => base.CppElement;
            set
            {
                base.CppElement = value;
                CppSignature = CppElement.ToString();
                ShortName = CppElement.ToShortString();
                CallingConvention = GetCallingConvention((CppCallable)value);
            }
        }

        protected abstract int MaxSizeReturnParameter { get; }

        [ExcludeFromCodeCoverage(Reason = "Required for XML serialization.")]
        protected CsCallable()
        {

        }

        protected CsCallable(CppCallable callable)
        {
            CppElement = callable;
        }

        private List<CsParameter> _parameters;
        public List<CsParameter> Parameters
        {
            get { return _parameters ?? (_parameters = Items.OfType<CsParameter>().ToList()); }
        }

        public IEnumerable<CsParameter> PublicParameters => Items.OfType<CsParameter>()
            .Where(param => !param.IsUsedAsReturnType && (param.Relations?.Count ?? 0) == 0);

        [DataMember]
        public string CallingConvention { get; set; }

        private static string GetCallingConvention(CppCallable method)
        {
            switch (method.CallingConvention)
            {
                case CppCallingConvention.StdCall:
                    return "StdCall";
                case CppCallingConvention.CDecl:
                    return "Cdecl";
                case CppCallingConvention.ThisCall:
                    return "ThisCall";
                case CppCallingConvention.FastCall:
                    return "FastCall";
                default:
                    return "Winapi";
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
                // Workaround for https://github.com/dotnet/coreclr/issues/19474. This workaround is sufficient
                // for DirectX on Windows x86 and x64. It may produce incorrect code on other platforms depending
                // on the calling convention details.
                if ((ReturnValue.MarshalType ?? ReturnValue.PublicType) is CsStruct csStruct)
                {
                    return !csStruct.IsNativePrimitive && csStruct.Size > MaxSizeReturnParameter;
                }
                return false;
            }
        }

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);

            if (tag.MethodCheckReturnType.HasValue)
                CheckReturnType = tag.MethodCheckReturnType.Value;

            if (tag.ParameterUsedAsReturnType.HasValue)
                ForceReturnType = tag.ParameterUsedAsReturnType.Value;

            if (tag.AlwaysReturnHResult.HasValue)
                AlwaysReturnHResult = tag.AlwaysReturnHResult.Value;

            if (tag.RawPtr.HasValue)
                RequestRawPtr = tag.RawPtr.Value;
        }

        [DataMember]
        public bool RequestRawPtr { get; set; }

        [DataMember]
        public Dictionary<PlatformDetectionType, InteropMethodSignature> InteropSignatures { get; set; } = new Dictionary<PlatformDetectionType, InteropMethodSignature>();

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

        [DataMember]
        public bool SignatureOnly { get; set; }

        public bool HasReturnType
        {
            get { return !(ReturnValue.PublicType is CsFundamentalType fundamental && fundamental.Type == typeof(void)); }
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
        public CsReturnValue ReturnValue { get; set; }


        /// <summary>
        /// Returns true if a parameter is marked to be used as the return type.
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

                return ReturnValue.PublicType.QualifiedName;
            }
        }

        /// <summary>
        /// Returns the documentation for the return type
        /// </summary>
        public string GetReturnTypeDoc(IDocumentationLinker linker)
        {
            foreach (var param in Parameters)
            {
                if (param.IsUsedAsReturnType)
                {
                    return linker.GetSingleDoc(param);
                }
            }
            return linker.GetSingleDoc(ReturnValue);
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
                return ReturnValue.Name;
            }
        }

        public override object Clone()
        {
            var method = (CsCallable)base.Clone();

            // Clear cached parameters
            method._parameters = null;
            method.ResetItems();
            foreach (var parameter in Parameters)
                method.Add((CsParameter)parameter.Clone());
            method.Parent = null;
            return method;
        }
    }
}
