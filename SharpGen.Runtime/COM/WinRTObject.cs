using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime.Win32;

namespace SharpGen.Runtime;

/// <unmanaged>IInspectable</unmanaged>
/// <unmanaged-short>IInspectable</unmanaged-short>
[Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
public class WinRTObject : ComObject, IInspectable
{
    public WinRTObject(IntPtr nativePtr): base(nativePtr)
    {
    }

    public static explicit operator WinRTObject(IntPtr nativePtr) => nativePtr == IntPtr.Zero ? null : new WinRTObject(nativePtr);

    /// <unmanaged>HRESULT IInspectable::GetTrustLevel([Out] TrustLevel* trustLevel)</unmanaged>
    /// <unmanaged-short>IInspectable::GetTrustLevel</unmanaged-short>
    private TrustLevel TrustLevel { get => GetTrustLevel(); }

    /// <unmanaged>HRESULT IInspectable::GetIids([Out] ULONG* iidCount, [Out, Buffer, Optional] GUID** iids)</unmanaged>
    /// <unmanaged-short>IInspectable::GetIids</unmanaged-short>
    private unsafe void GetIids(out uint iidCount, out IntPtr iids)
    {
        Result __result__;
        fixed (void* iids_ = &iids)
        fixed (void* iidCount_ = &iidCount)
            __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, void*, void*, int> )this[3U])(NativePointer, iidCount_, iids_);
        __result__.CheckError();
    }

    /// <unmanaged>HRESULT IInspectable::GetRuntimeClassName([Out] HSTRING* className)</unmanaged>
    /// <unmanaged-short>IInspectable::GetRuntimeClassName</unmanaged-short>
    private unsafe IntPtr GetRuntimeClassName()
    {
        IntPtr className;
        Result __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, void*, int> )this[4U])(NativePointer, &className);
        __result__.CheckError();
        return className;
    }

    /// <unmanaged>HRESULT IInspectable::GetTrustLevel([Out] TrustLevel* trustLevel)</unmanaged>
    /// <unmanaged-short>IInspectable::GetTrustLevel</unmanaged-short>
    private unsafe TrustLevel GetTrustLevel()
    {
        TrustLevel trustLevel;
        Result __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, void*, int> )this[5U])(NativePointer, &trustLevel);
        __result__.CheckError();
        return trustLevel;
    }

    public Guid[] Iids
    {
        get
        {
            GetIids(out var count, out var iids);
            var iid = new Guid[count];
            MemoryHelpers.Read<Guid>(iids, iid, (int) count);
            Marshal.FreeCoTaskMem(iids);
            return iid;
        }
    }

    public string RuntimeClassName
    {
        get
        {
            var nativeStringPtr = GetRuntimeClassName();
            using WinRTString nativeString = new(nativeStringPtr);
            return nativeString.Value;
        }
    }
}