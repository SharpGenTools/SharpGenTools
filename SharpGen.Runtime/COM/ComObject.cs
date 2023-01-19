// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpGen.Runtime.Diagnostics;

namespace SharpGen.Runtime;

/// <unmanaged>IUnknown</unmanaged>
/// <unmanaged-short>IUnknown</unmanaged-short>
[Guid("00000000-0000-0000-C000-000000000046")]
public class ComObject : CppObject, IUnknown
{
    public ComObject(IntPtr nativePtr) : base(nativePtr)
    {
    }

    public static explicit operator ComObject?(IntPtr nativePtr)
    {
        return nativePtr == IntPtr.Zero ? null : new ComObject(nativePtr);
    }

    /// <unmanaged>HRESULT IUnknown::QueryInterface([In] const GUID&amp; riid, [Out] void** ppvObject)</unmanaged>
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public unsafe Result QueryInterface(Guid riid, out IntPtr ppvObject)
    {
        Result __result__;
        fixed (void* ppvObject_ = &ppvObject)
            __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, void*, void*, int>) this[0U])(NativePointer, &riid, ppvObject_);
        return __result__;
    }

    /// <unmanaged>ULONG IUnknown::AddRef()</unmanaged>
    /// <unmanaged-short>IUnknown::AddRef</unmanaged-short>
    public unsafe uint AddRef()
    {
        uint __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, uint>) this[1U])(NativePointer);
        return __result__;
    }

    /// <unmanaged>ULONG IUnknown::Release()</unmanaged>
    /// <unmanaged-short>IUnknown::Release</unmanaged-short>
    public unsafe uint Release()
    {
        uint __result__;
        __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, uint>) this[2U])(NativePointer);
        return __result__;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComObject"/> class from a IUnknown object.
    /// </summary>
    /// <param name="iunknownObject">Reference to a IUnknown object</param>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public ComObject(object iunknownObject) : base(Marshal.GetIUnknownForObject(iunknownObject))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComObject"/> class.
    /// </summary>
    protected ComObject()
    {
    }

    /// <summary>
    ///   Query instance for a particular COM GUID/interface support.
    /// </summary>
    /// <param name = "guid">GUID query interface</param>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public virtual IntPtr QueryInterfaceOrNull(Guid guid)
    {
        QueryInterface(guid, out var pointer);
        return pointer;
    }

    ///<summary>
    /// Query this instance for a particular COM interface support.
    ///</summary>
    ///<typeparam name="T">The type of the COM interface to query</typeparam>
    ///<returns>An instance of the queried interface</returns>
    /// <exception cref="SharpGenException">If this object doesn't support the interface</exception>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public virtual T QueryInterface<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>() where T : ComObject
    {
        QueryInterface(typeof(T).GetTypeInfo().GUID, out var parentPtr).CheckError();
        return MarshallingHelpers.FromPointer<T>(parentPtr)!;
    }

    /// <summary>
    /// Queries a managed object for a particular COM interface support (This method is a shortcut to <see cref="QueryInterface"/>)
    /// </summary>
    ///<typeparam name="T">The type of the COM interface to query</typeparam>
    /// <param name="comObject">The managed COM object.</param>
    ///<returns>An instance of the queried interface</returns>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static T As<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(object comObject) where T : ComObject => As<T>(Marshal.GetIUnknownForObject(comObject));

    /// <summary>
    /// Queries a managed object for a particular COM interface support (This method is a shortcut to <see cref="QueryInterface"/>)
    /// </summary>
    ///<typeparam name="T">The type of the COM interface to query</typeparam>
    /// <param name="iunknownPtr">The managed COM object.</param>
    ///<returns>An instance of the queried interface</returns>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public static T As<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(IntPtr iunknownPtr) where T : ComObject
    {
        using var tempObject = new ComObject(iunknownPtr);
        return tempObject.QueryInterface<T>();
    }

    /// <summary>
    /// Queries a managed object for a particular COM interface support.
    /// </summary>
    ///<typeparam name="T">The type of the COM interface to query</typeparam>
    /// <param name="comObject">The managed COM object.</param>
    ///<returns>An instance of the queried interface</returns>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static T QueryInterface<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(object comObject) where T : ComObject =>
        As<T>(Marshal.GetIUnknownForObject(comObject));

    /// <summary>
    /// Queries a managed object for a particular COM interface support.
    /// </summary>
    ///<typeparam name="T">The type of the COM interface to query</typeparam>
    /// <param name="comPointer">A pointer to a COM object.</param>
    ///<returns>An instance of the queried interface</returns>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public static T? QueryInterfaceOrNull<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(IntPtr comPointer) where T : ComObject
    {
        using var tempObject = new ComObject(comPointer);
        return tempObject.QueryInterfaceOrNull<T>();
    }

    ///<summary>
    /// Query Interface for a particular interface support.
    ///</summary>
    ///<returns>An instance of the queried interface or null if it is not supported</returns>
    ///<returns></returns>
    /// <msdn-id>ms682521</msdn-id>
    /// <unmanaged>IUnknown::QueryInterface</unmanaged>	
    /// <unmanaged-short>IUnknown::QueryInterface</unmanaged-short>
    public virtual T? QueryInterfaceOrNull<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>() where T : ComObject
    {
        return MarshallingHelpers.FromPointer<T>(QueryInterfaceOrNull(typeof(T).GetTypeInfo().GUID));
    }

    ///<summary>
    /// Query Interface for a particular interface support and attach to the given instance.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    ///<returns></returns>
    protected void QueryInterfaceFrom<T>(T fromObject) where T : ComObject
    {
        NativePointer = fromObject.QueryInterfaceOrNull(GetType().GetTypeInfo().GUID);
    }

    protected override void DisposeCore(IntPtr nativePointer, bool disposing)
    {
        // Release the object
        if (disposing || ObjectTrackerReadOnlyConfiguration.IsReleaseOnFinalizerEnabled)
            Release();
    }
}