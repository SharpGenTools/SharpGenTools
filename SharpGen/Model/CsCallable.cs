using System;
using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGen.Model;

public abstract class CsCallable : CsBase, IExpiring
{
    private CsBaseItemListCache<CsParameter> _parameters;
    private Dictionary<PlatformDetectionType, InteropMethodSignature> interopSignatures;
    protected readonly Ioc Ioc;

    protected abstract int MaxSizeReturnParameter { get; }

    protected CsCallable(Ioc ioc, CppCallable callable, string name) : base(callable, name)
    {
        Ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));

        if (callable == null)
            return;

        var tag = callable.Rule;
        CheckReturnType = tag.MethodCheckReturnType ?? CheckReturnType;
        ForceReturnType = tag.ParameterUsedAsReturnType ?? ForceReturnType;
        AlwaysReturnHResult = tag.AlwaysReturnHResult ?? AlwaysReturnHResult;
        RequestRawPtr = tag.RawPtr ?? RequestRawPtr;

        CppSignature = callable.ToString();
        ShortName = callable.ToShortString();
        CppCallingConvention = callable.CallingConvention;
    }

    public CppCallingConvention CppCallingConvention { get; } = CppCallingConvention.Unknown;
    public bool RequestRawPtr { get; }
    private string CppSignature { get; }
    private string ShortName { get; }
    public bool CheckReturnType { get; } = true;
    public bool ForceReturnType { get; }
    private bool AlwaysReturnHResult { get; }
    public CsReturnValue ReturnValue { get; set; }
    public bool SignatureOnly => GetParent<CsInterface>()?.IsCallback ?? false;
    public override string DocUnmanagedName => CppSignature ?? "Unknown";
    public override string DocUnmanagedShortName => ShortName;

    public IReadOnlyList<CsParameter> Parameters => _parameters.GetList(this);

    public IEnumerable<CsParameter> PublicParameters => _parameters.Enumerate(this)
                                                                   .Where(param => !param.UsedAsReturn && param.Relations.Count == 0);

    public IEnumerable<CsParameter> InRefInRefParameters => _parameters.Enumerate(this)
                                                                       .Where(param => !param.IsOut);

    public IEnumerable<CsParameter> LocalManagedReferenceParameters => _parameters.Enumerate(this)
       .Where(param => param.IsLocalManagedReference);

    public CsMarshalCallableBase ActualReturnValue
    {
        get
        {
            foreach (var param in _parameters.Enumerate(this).Where(param => param.UsedAsReturn))
                return param;

            return ReturnValue;
        }
    }

    public override void FillDocItems(IList<string> docItems, IDocumentationLinker manager)
    {
        string doc;
        foreach (var param in PublicParameters)
        {
            doc = manager.GetSingleDoc(param);
            if (string.IsNullOrEmpty(doc) || doc == DefaultNoDescription)
                continue;

            docItems.Add("<param name=\"" + param.Name + "\">" + doc + "</param>");
        }

        if (!HasReturnType)
            return;

        doc = manager.GetSingleDoc(ActualReturnValue);
        if (string.IsNullOrEmpty(doc) || doc == DefaultNoDescription)
            return;

        docItems.Add("<returns>" + doc + "</returns>");
    }

    // Workaround for https://github.com/dotnet/runtime/issues/10901. This workaround is sufficient
    // for DirectX on Windows x86 and x64. It may produce incorrect code on other platforms depending
    // on the calling convention details.
    public bool IsReturnStructLarge => ReturnValue.MarshalType is CsStruct { IsNativePrimitive: false } csStruct
                                    && csStruct.Size > MaxSizeReturnParameter;

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
    public bool IsReturnTypeHidden => CheckReturnType && IsReturnTypeResult && !AlwaysReturnHResult;
    public bool IsReturnTypeResult => ReturnValue.PublicType.IsWellKnownType(Ioc.GlobalNamespace, WellKnownName.Result);
    public bool HasReturnType => ReturnValue.PublicType != TypeRegistry.Void;
    internal bool HasReturnStatement => HasReturnTypeParameter || HasReturnTypeValue;
    internal bool HasReturnTypeValue => HasReturnType && (ForceReturnType || !IsReturnTypeHidden);

    /// <summary>
    /// Returns true if a parameter is marked to be used as the return type.
    /// </summary>
    public bool HasReturnTypeParameter => _parameters.Enumerate(this).Any(param => param.UsedAsReturn);

    /// <summary>
    /// Return the Public return type. If a out parameter is used as a public return type
    /// then use the type of the out parameter for the public API.
    /// </summary>
    public string PublicReturnTypeQualifiedName
    {
        get
        {
            var returnValue = ActualReturnValue;
            if (returnValue is CsParameter)
                return returnValue.PublicType.QualifiedName;

            if (IsReturnTypeHidden && !ForceReturnType)
                return "void";

            return returnValue.PublicType.QualifiedName;
        }
    }

    /// <summary>
    /// Return the name of the variable used to return the value
    /// </summary>
    public string ReturnName => ActualReturnValue.Name;

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
        method.Expire();
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