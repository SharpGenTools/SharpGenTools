using System;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsGuidConstant : CsConstantBase
    {
        public CsGuidConstant(CppElement cppElement, string name, Guid value) : base(cppElement, name) => Value = value;

        protected override Visibility DefaultVisibility => Visibility.Public | Visibility.Static | Visibility.Readonly;

        public Guid Value { get; }

        protected override string DefaultDescription => $"GUID {Name}";
    }
}