using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

#pragma warning disable 0618

namespace SharpGen.VisualStudioSetup
{
  [Guid("89143C9A-05AF-49B0-B717-72E218A2185C")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface ISetupInstance2 : ISetupInstance
  {
    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetInstanceId();
        
    new FILETIME GetInstallDate();

    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetInstallationName();

    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetInstallationPath();

    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetInstallationVersion();

    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetDisplayName([MarshalAs(UnmanagedType.U4), In] int lcid = 0);

    [return: MarshalAs(UnmanagedType.BStr)]
    new string GetDescription([MarshalAs(UnmanagedType.U4), In] int lcid = 0);

    [return: MarshalAs(UnmanagedType.BStr)]
    new string ResolvePath([MarshalAs(UnmanagedType.LPWStr), In] string pwszRelativePath = null);

    [return: MarshalAs(UnmanagedType.U4)]
    InstanceState GetState();

    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
    ISetupPackageReference[] GetPackages();

    ISetupPackageReference GetProduct();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetProductPath();

    ISetupErrorState GetErrors();

    [return: MarshalAs(UnmanagedType.VariantBool)]
    bool IsLaunchable();

    [return: MarshalAs(UnmanagedType.VariantBool)]
    bool IsComplete();

    ISetupPropertyStore GetProperties();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetEnginePath();
  }
}
