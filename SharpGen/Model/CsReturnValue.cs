using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsReturnValue : CsMarshalCallableBase
    {
        public CsReturnValue(Ioc ioc, CppReturnValue cppReturnValue) : base(ioc, cppReturnValue, "__result__")
        {
        }

        public static string MarshalStorageLocation => "__result__native";

        public override bool IsOut => true;
        public override bool IsFixed => false;
        public override bool IsLocalManagedReference => true;
        public override bool UsedAsReturn => true;
        public override bool PassedByNativeReference => true;
    }
}
