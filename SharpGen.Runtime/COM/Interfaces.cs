using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

/// <summary>
/// No documentation.
/// </summary>
/// <unmanaged>IUnknown</unmanaged>
/// <unmanaged-short>IUnknown</unmanaged-short>
[Guid("00000000-0000-0000-C000-000000000046")]
public partial class ComObject : CppObject, IUnknown
{
    public ComObject(IntPtr nativePtr): base(nativePtr)
    {
    }

    public static explicit operator ComObject(IntPtr nativePtr) => nativePtr == IntPtr.Zero ? null : new ComObject(nativePtr);
    /// <summary>
    /// No documentation.
    /// </summary>
    /// <param name = "riid">No documentation.</param>
    /// <param name = "ppvObject">No documentation.</param>
    /// <returns>No documentation.</returns>
    /// <unmanaged>HRESULT IUnknown::QueryInterface([In] const GUID&amp; riid, [Out] void** ppvObject)</unmanaged>
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public unsafe Result QueryInterface(Guid riid, out IntPtr ppvObject)
    {
        Result __result__;
        fixed (void* ppvObject_ = &ppvObject)
            __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, void*, void*, int> )this[0U])(NativePointer, &riid, ppvObject_);
        return __result__;
    }

    /// <summary>
    /// No documentation.
    /// </summary>
    /// <returns>No documentation.</returns>
    /// <unmanaged>ULONG IUnknown::AddRef()</unmanaged>
    /// <unmanaged-short>IUnknown::AddRef</unmanaged-short>
    public unsafe uint AddRef()
    {
        uint __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, uint> )this[1U])(NativePointer);
        return __result__;
    }

    /// <summary>
    /// No documentation.
    /// </summary>
    /// <returns>No documentation.</returns>
    /// <unmanaged>ULONG IUnknown::Release()</unmanaged>
    /// <unmanaged-short>IUnknown::Release</unmanaged-short>
    public unsafe uint Release()
    {
        uint __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, uint> )this[2U])(NativePointer);
        return __result__;
    }
}

public class InspectableShadow : ComObjectShadow
{
    private static readonly InspectableVtbl VtblInstance = new(0);
    protected override CppObjectVtbl Vtbl => VtblInstance;
}

/// <summary>
/// No documentation.
/// </summary>
/// <unmanaged>IInspectable</unmanaged>
/// <unmanaged-short>IInspectable</unmanaged-short>
[Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90"), ShadowAttribute(typeof(InspectableShadow))]
public partial interface IInspectable : IUnknown
{
}

public class ComObjectShadow : CppObjectShadow
{
    private static readonly ComObjectVtbl VtblInstance = new(0);
    protected override CppObjectVtbl Vtbl => VtblInstance;
}

/// <summary>
/// No documentation.
/// </summary>
/// <unmanaged>IUnknown</unmanaged>
/// <unmanaged-short>IUnknown</unmanaged-short>
[Guid("00000000-0000-0000-C000-000000000046"), ShadowAttribute(typeof(ComObjectShadow))]
public partial interface IUnknown : ICallbackable
{
}

/// <summary>
/// No documentation.
/// </summary>
/// <unmanaged>IInspectable</unmanaged>
/// <unmanaged-short>IInspectable</unmanaged-short>
[Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
public partial class WinRTObject : ComObject, IInspectable
{
    public WinRTObject(IntPtr nativePtr): base(nativePtr)
    {
    }

    public static explicit operator WinRTObject(IntPtr nativePtr) => nativePtr == IntPtr.Zero ? null : new WinRTObject(nativePtr);
    /// <summary>
    /// No documentation.
    /// </summary>
    /// <unmanaged>HRESULT IInspectable::GetTrustLevel([Out] TrustLevel* trustLevel)</unmanaged>
    /// <unmanaged-short>IInspectable::GetTrustLevel</unmanaged-short>
    private TrustLevel TrustLevel { get => GetTrustLevel(); }

    /// <summary>
    /// No documentation.
    /// </summary>
    /// <param name = "iidCount">No documentation.</param>
    /// <param name = "iids">No documentation.</param>
    /// <returns>No documentation.</returns>
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

    /// <summary>
    /// No documentation.
    /// </summary>
    /// <returns>No documentation.</returns>
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

    /// <summary>
    /// No documentation.
    /// </summary>
    /// <returns>No documentation.</returns>
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
}