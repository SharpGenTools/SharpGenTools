using System.Runtime.CompilerServices;
using SharpGen.Runtime;

namespace SharpGen.UnitTests.Runtime;

internal static class TestTypeRegistry
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        TypeDataStorage.Storage<ICallback>.SourceVtbl = CallbackVtbl.Vtbl;
        TypeDataStorage.Storage<ICallback2>.SourceVtbl = Callback2Vtbl.Vtbl;
        TypeDataRegistrationHelper helper = new();
        {
            helper.Add<ICallback>();
            helper.Register<ICallback>();
        }
        {
            helper.Add<ICallback>();
            helper.Add<ICallback2>();
            helper.Register<ICallback2>();
        }
    }
}