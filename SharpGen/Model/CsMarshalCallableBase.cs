using SharpGen.CppModel;

namespace SharpGen.Model
{
    public abstract class CsMarshalCallableBase : CsMarshalBase
    {
        public virtual bool UsedAsReturn => false;
        public virtual bool IsRef => false;
        public virtual bool IsOut => false;
        public virtual bool IsFixed => false;
        public virtual bool PassedByNativeReference => false;

        protected CsMarshalCallableBase(CppMarshallable cppElement, string name) : base(cppElement, name)
        {
        }
    }
}
