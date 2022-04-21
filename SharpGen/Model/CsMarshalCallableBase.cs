using SharpGen.CppModel;

namespace SharpGen.Model;

public abstract class CsMarshalCallableBase : CsMarshalBase
{
    public abstract bool UsedAsReturn { get; }
    public abstract bool IsOut { get; }
    public abstract bool IsFixed { get; }
    public abstract bool IsLocalManagedReference { get; }
    public abstract bool PassedByNativeReference { get; }

    protected CsMarshalCallableBase(Ioc ioc, CppMarshallable cppElement, string name) : base(ioc, cppElement, name)
    {
    }
}