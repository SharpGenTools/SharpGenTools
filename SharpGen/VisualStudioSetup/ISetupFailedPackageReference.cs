using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

#pragma warning disable 0618

namespace SharpGen.VisualStudioSetup
{
    [Guid("E73559CD-7003-4022-B134-27DC650B280F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ISetupFailedPackageReference : ISetupPackageReference
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetId();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetVersion();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetChip();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetLanguage();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetBranch();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetType();

        [return: MarshalAs(UnmanagedType.BStr)]
        new string GetUniqueId();

        [return: MarshalAs(UnmanagedType.VariantBool)]
        new bool GetIsExtension();
    }
}
