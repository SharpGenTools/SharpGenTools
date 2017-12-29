
using System.Runtime.InteropServices;

namespace SharpGen.E2ETests.VisualStudio
{
  [Guid("42843719-DB4C-46C2-8E7C-64F1816EFD5B")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface ISetupConfiguration
  {
    [return: MarshalAs(UnmanagedType.Interface)]
    IEnumSetupInstances EnumInstances();

    [return: MarshalAs(UnmanagedType.Interface)]
    ISetupInstance GetInstanceForCurrentProcess();

    [return: MarshalAs(UnmanagedType.Interface)]
    ISetupInstance GetInstanceForPath([MarshalAs(UnmanagedType.LPWStr), In] string path);
  }
}
