using SharpGen.CppModel;

namespace SharpGen.Model
{
    public sealed class CsReturnValue : CsMarshalCallableBase
    {
        public CsReturnValue(CppReturnValue cppReturnValue) : base(cppReturnValue, "__result__")
        {
        }

        public static string MarshalStorageLocation => "__result__native";

        public override bool IsOut => true;

        public override bool UsedAsReturn => true;

        public override bool PassedByNativeReference => true;
    }
}
