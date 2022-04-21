using System;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model;

public sealed class CsExpressionConstant : CsConstantBase
{
    public CsExpressionConstant(CppElement cppElement, string name, CsTypeBase type, string value) : base(cppElement, name)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected override Visibility DefaultVisibility => Visibility.Public | Visibility.Const;

    public CsTypeBase Type { get; }
    public string Value { get; }

    protected override string DefaultDescription => $"Constant {Name}";
}