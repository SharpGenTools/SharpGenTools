using SharpGen.CppModel;

namespace SharpGen.Model
{
    public abstract class CsConstantBase : CsBase
    {
        protected CsConstantBase(CppElement cppElement, string name) : base(cppElement, name)
        {
        }
    }
}