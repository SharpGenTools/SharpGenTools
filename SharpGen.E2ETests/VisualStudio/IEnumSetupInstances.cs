
using System.Runtime.InteropServices;

namespace SharpGen.E2ETests.VisualStudio
{
  [Guid("6380BCFF-41D3-4B2E-8B2E-BF8A6810C848")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface IEnumSetupInstances
  {
    void Next([MarshalAs(UnmanagedType.U4), In] int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface), Out] ISetupInstance[] rgelt, [MarshalAs(UnmanagedType.U4)] out int pceltFetched);

    void Skip([MarshalAs(UnmanagedType.U4), In] int celt);

    void Reset();

    [return: MarshalAs(UnmanagedType.Interface)]
    IEnumSetupInstances Clone();
  }
}
