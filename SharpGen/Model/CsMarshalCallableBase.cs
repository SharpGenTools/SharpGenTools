using SharpGen.CppModel;

namespace SharpGen.Model
{
    public abstract class CsMarshalCallableBase : CsMarshalBase
    {
        public abstract bool UsedAsReturn { get; }
        public abstract bool IsOut { get; }
        public abstract bool IsFixed { get; }
        public abstract bool IsLocalByRef { get; }
        public abstract bool PassedByNativeReference { get; }

        protected CsMarshalCallableBase(CppMarshallable cppElement, string name) : base(cppElement, name)
        {
        }
    }
}
