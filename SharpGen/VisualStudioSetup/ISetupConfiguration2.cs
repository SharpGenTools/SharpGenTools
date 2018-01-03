
using System.Runtime.InteropServices;

namespace SharpGen.VisualStudioSetup
{
  [Guid("26AAB78C-4A60-49D6-AF3B-3C35BC93365D")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface ISetupConfiguration2 : ISetupConfiguration
  {
    [return: MarshalAs(UnmanagedType.Interface)]
    new IEnumSetupInstances EnumInstances();

    [return: MarshalAs(UnmanagedType.Interface)]
    new ISetupInstance GetInstanceForCurrentProcess();

    [return: MarshalAs(UnmanagedType.Interface)]
    new ISetupInstance GetInstanceForPath([MarshalAs(UnmanagedType.LPWStr), In] string path);

    [return: MarshalAs(UnmanagedType.Interface)]
    IEnumSetupInstances EnumAllInstances();
  }
}
