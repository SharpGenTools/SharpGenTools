using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsResultConstant : CsConstantBase
    {
        public CsResultConstant(CppElement cppElement, string name, string value, string module) : base(cppElement, name)
        {
            Value = value;
            Module = module;
        }

        protected override Visibility DefaultVisibility => Visibility.Public | Visibility.Static | Visibility.Readonly;

        public string Module { get; }
        public string Value { get; }

        protected override string DefaultDescription => $"Result {Name}";
    }
}