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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model;

public sealed class CsInterface : CsTypeBase
{
    private CsBaseItemListCache<CsMethod> methods;
    private string shadowName;
    private string vtblName;

    public CsInterface(CppInterface cppInterface, string name) : base(cppInterface, name)
    {
        if (cppInterface == null)
            return;

        var tag = cppInterface.Rule;
        IsCallback = tag.IsCallbackInterface ?? IsCallback;
        IsDualCallback = tag.IsDualCallbackInterface ?? IsDualCallback;
        AutoGenerateShadow = tag.AutoGenerateShadow ?? AutoGenerateShadow;
        AutoGenerateVtbl = tag.AutoGenerateVtbl ?? AutoGenerateVtbl;
        StaticShadowVtbl = tag.StaticShadowVtbl ?? StaticShadowVtbl;
        ShadowVisibility = tag.ShadowVisibility ?? ShadowVisibility;
        VtblVisibility = tag.VtblVisibility ?? VtblVisibility;
        AutoDisposePersistentProperties = tag.AutoDisposePersistentProperties ?? AutoDisposePersistentProperties;

        if (tag.ShadowName != null)
            ShadowName = tag.ShadowName;
        if (tag.VtblName != null)
            VtblName = tag.VtblName;

        Guid = FindGuid(cppInterface);
    }

    private static string FindGuid(CppInterface cppInterface)
    {
        if (!string.IsNullOrEmpty(cppInterface.Guid))
            return cppInterface.Guid;

        // If Guid is null we try to recover it from a declared GUID

        var finder = new CppElementFinder(cppInterface.ParentInclude);

        var cppGuid = finder.Find<CppGuid>("IID_" + cppInterface.Name).FirstOrDefault();

        return cppGuid != null
                   ? cppGuid.Guid.ToString()
                   : cppInterface.Guid;
    }

    public IEnumerable<CsMethod> Methods => methods.Enumerate(this);
    public IReadOnlyList<CsMethod> MethodList => methods.GetList(this);

    public IEnumerable<CsProperty> Properties => Items.OfType<CsProperty>();
    public IEnumerable<CsExpressionConstant> ExpressionConstants => Items.OfType<CsExpressionConstant>();
    public IEnumerable<CsGuidConstant> GuidConstants => Items.OfType<CsGuidConstant>();
    public IEnumerable<CsResultConstant> ResultConstants => Items.OfType<CsResultConstant>();

    /// <summary>
    /// Gets or sets the <see cref="Visibility"/> of the Shadow of this interface.
    /// Default is empty, if no <c>partial</c> part is present it will be <c>internal</c> (top-level type).
    /// </summary>
    public Visibility? ShadowVisibility { get; }

    /// <summary>
    /// Gets or sets the <see cref="Visibility"/> of the Vtbl of this interface.
    /// Default is <c>protected internal</c>.
    /// </summary>
    public Visibility? VtblVisibility { get; }

    /// <summary>
    /// Class Parent inheritance
    /// </summary>
    public CsInterface Base { get; set; }

    /// <summary>
    /// Interface Parent inheritance
    /// </summary>
    public CsInterface IBase { get; set; }

    public CsInterface NativeImplementation { get; set; }

    public string Guid { get; }

    /// <summary>
    ///   Only valid for inner interface. Specify the name of the property in the outer interface to access to the inner interface
    /// </summary>
    public string PropertyAccessName { get; set; }

    /// <summary>
    ///   True if this interface is used as a callback to a C# object
    /// </summary>
    public bool IsCallback { get; set; }

    /// <summary>
    ///   True if this interface is used as a dual-callback to a C# object
    /// </summary>
    public bool IsDualCallback { get; set; }

    public string ShadowName
    {
        get => shadowName ?? DefaultShadowFullName;
        set
        {
            shadowName = value;
            AutoGenerateShadow = true;
        }
    }

    public string VtblName
    {
        get => vtblName ?? DefaultVtblFullName;
        set => vtblName = value;
    }

    private string DefaultShadowFullName => $"{QualifiedName}Shadow";
    private string DefaultVtblFullName => $"{QualifiedName}Vtbl";

    public bool AutoGenerateShadow { get; private set; }
    public bool AutoGenerateVtbl { get; } = true;
    public bool StaticShadowVtbl { get; } = true;
    public bool AutoDisposePersistentProperties { get; } = true;

    public bool HasInnerInterfaces => InnerInterfaces.Any();

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture, "csinterface {0} => {1}",
            CppElementName, QualifiedName
        );
    }

    public IEnumerable<CsInterface> InnerInterfaces => Items.OfType<CsInterface>();

    public bool IsFullyMapped { get; set; } = true;

    private protected override IEnumerable<IExpiring> ExpiringOnItemsChange
    {
        get
        {
            yield return methods.Expiring;
        }
    }

    public override bool IsBlittable => true;
}