
using System.Runtime.InteropServices;

#pragma warning disable 0618

namespace SharpGen.VisualStudioSetup
{
  [Guid("DA8D8A16-B2B6-4487-A2F1-594CCCCD6BF5")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface ISetupPackageReference
  {
    [return: MarshalAs(UnmanagedType.BStr)]
    string GetId();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetVersion();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetChip();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetLanguage();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetBranch();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetType();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetUniqueId();

    [return: MarshalAs(UnmanagedType.VariantBool)]
    bool GetIsExtension();
  }
}
