using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGen.Model
{
    public abstract class CsCallable : CsBase, IExpiring
    {
        private CsBaseItemListCache<CsParameter> _parameters;
        private Dictionary<PlatformDetectionType, InteropMethodSignature> interopSignatures;

        protected abstract int MaxSizeReturnParameter { get; }

        protected CsCallable(CppCallable callable, string name) : base(callable, name)
        {
            var tag = callable?.Rule;
            CheckReturnType = tag?.MethodCheckReturnType ?? CheckReturnType;
            ForceReturnType = tag?.ParameterUsedAsReturnType ?? ForceReturnType;
            AlwaysReturnHResult = tag?.AlwaysReturnHResult ?? AlwaysReturnHResult;
            RequestRawPtr = tag?.RawPtr ?? RequestRawPtr;

            if (callable == null)
                return;

            CppSignature = callable.ToString();
            ShortName = callable.ToShortString();
            CppCallingConvention = callable.CallingConvention;
        }

        public CallingConvention CppCallingConvention { get; } = CallingConvention.Winapi;
        public bool RequestRawPtr { get; }
        private string CppSignature { get; }
        private string ShortName { get; }
        public bool CheckReturnType { get; }
        public bool ForceReturnType { get; }
        private bool AlwaysReturnHResult { get; }
        public CsReturnValue ReturnValue { get; set; }
        public bool SignatureOnly => GetParent<CsInterface>()?.IsCallback ?? false;
        public override string DocUnmanagedName => CppSignature ?? "Unknown";
        public override string DocUnmanagedShortName => ShortName;

        public IReadOnlyList<CsParameter> Parameters => _parameters.GetList(this);

        public IEnumerable<CsParameter> PublicParameters => _parameters.Enumerate(this)
           .Where(param => !param.IsUsedAsReturnType && (param.Relations?.Count ?? 0) == 0);

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

        public Dictionary<PlatformDetectionType, InteropMethodSignature> InteropSignatures
        {
            get => interopSignatures ?? throw new InvalidOperationException($"Accessing non-initialized {nameof(InteropSignatures)}");
            set
            {
                if (interopSignatures != null)
                    throw new InvalidOperationException($"Setting initialized {nameof(InteropSignatures)}");

                interopSignatures = value;
            }
        }

        // Hide return type only if it is a HRESULT and AlwaysReturnHResult is false
        public bool IsReturnTypeHidden(GlobalNamespaceProvider globalNamespace) =>
            CheckReturnType && IsReturnTypeResult(globalNamespace) && !AlwaysReturnHResult;

        public bool IsReturnTypeResult(GlobalNamespaceProvider globalNamespace) =>
            ReturnValue.PublicType?.IsWellKnownType(globalNamespace, WellKnownName.Result) ?? false;

        public bool HasReturnType => ReturnValue.PublicType != TypeRegistry.Void;

        internal bool HasReturnStatement(GlobalNamespaceProvider globalNamespace) =>
            HasReturnTypeParameter || HasReturnTypeValue(globalNamespace);

        internal bool HasReturnTypeValue(GlobalNamespaceProvider globalNamespace) =>
            HasReturnType && (ForceReturnType || !IsReturnTypeHidden(globalNamespace));

        /// <summary>
        /// Returns true if a parameter is marked to be used as the return type.
        /// </summary>
        public bool HasReturnTypeParameter => Parameters.Any(param => param.IsUsedAsReturnType);

        /// <summary>
        /// Return the Public return type. If a out parameter is used as a public return type
        /// then use the type of the out parameter for the public API.
        /// </summary>
        public string GetPublicReturnTypeQualifiedName(GlobalNamespaceProvider globalNamespace)
        {
            foreach (var param in Parameters.Where(param => param.IsUsedAsReturnType))
            {
                return param.PublicType.QualifiedName;
            }

            if (IsReturnTypeHidden(globalNamespace) && !ForceReturnType)
                return "void";

            return ReturnValue.PublicType.QualifiedName;
        }

        /// <summary>
        /// Returns the documentation for the return type
        /// </summary>
        protected string GetReturnTypeDoc(IDocumentationLinker linker)
        {
            foreach (var param in Parameters.Where(param => param.IsUsedAsReturnType))
            {
                return linker.GetSingleDoc(param);
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
                foreach (var param in Parameters.Where(param => param.IsUsedAsReturnType))
                {
                    return param.Name;
                }

                return ReturnValue.Name;
            }
        }

        private protected override IEnumerable<IExpiring> ExpiringOnItemsChange
        {
            get
            {
                yield return _parameters.Expiring;
                yield return this;
            }
        }

        public virtual CsCallable Clone()
        {
            var method = (CsCallable) MemberwiseClone();

            // Clear cached parameters
            method.interopSignatures = null;
            method.ResetItems();
            foreach (var parameter in Parameters)
                method.Add(parameter.Clone());
            method.ResetParentAfterClone();
            return method;
        }

        public void Expire()
        {
            interopSignatures = null;
        }

        public override IEnumerable<CsBase> AdditionalItems => AppendNonNull(base.AdditionalItems, ReturnValue);
    }
}
