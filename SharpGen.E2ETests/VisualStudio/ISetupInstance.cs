
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SharpGen.E2ETests.VisualStudio
{
  [Guid("B41463C3-8866-43B5-BC33-2B0676F7F42E")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface ISetupInstance
  {
    [return: MarshalAs(UnmanagedType.BStr)]
    string GetInstanceId();
        
    FILETIME GetInstallDate();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetInstallationName();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetInstallationPath();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetInstallationVersion();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetDisplayName([MarshalAs(UnmanagedType.U4), In] int lcid = 0);

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetDescription([MarshalAs(UnmanagedType.U4), In] int lcid = 0);

    [return: MarshalAs(UnmanagedType.BStr)]
    string ResolvePath([MarshalAs(UnmanagedType.LPWStr), In] string pwszRelativePath = null);
  }
}
