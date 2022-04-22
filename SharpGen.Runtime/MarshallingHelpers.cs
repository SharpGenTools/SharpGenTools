using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

public static partial class MarshallingHelpers
{
    /// <summary>
    /// Instantiate a CppObject from a native pointer.
    /// </summary>
    /// <typeparam name="T">The CppObject class that will be returned</typeparam>
    /// <param name="cppObjectPtr">The native pointer to a C++ object.</param>
    /// <returns>An instance of T bound to the native pointer</returns>
    public static T FromPointer<T>(IntPtr cppObjectPtr) where T : CppObject =>
        cppObjectPtr == IntPtr.Zero ? null : (T) Activator.CreateInstance(typeof(T), cppObjectPtr);

    /// <summary>
    /// Instantiate a CppObject from a native pointer.
    /// </summary>
    /// <typeparam name="T">The CppObject class that will be returned</typeparam>
    /// <param name="cppObjectPtr">The native pointer to a C++ object.</param>
    /// <returns>An instance of T bound to the native pointer</returns>
    public static T FromPointer<T>(UIntPtr cppObjectPtr) where T : CppObject =>
        cppObjectPtr == UIntPtr.Zero ? null : (T) Activator.CreateInstance(typeof(T), cppObjectPtr);

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static uint AddRef<TCallback>(TCallback callback) where TCallback : ICallbackable =>
        callback switch
        {
            null => throw new NullReferenceException(),
            ComObject cpp => cpp.AddRef(),
            CallbackBase managed => managed.AddRef(),
        };

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static uint Release<TCallback>(TCallback callback) where TCallback : ICallbackable =>
        callback switch
        {
            null => throw new NullReferenceException(),
            ComObject cpp => cpp.Release(),
            CallbackBase managed => managed.Release(),
        };

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.ICallbackable"/> instance.
    /// </summary>
    /// <typeparam name="TCallback">The type of the callback.</typeparam>
    /// <param name="callback">The callback.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    public static IntPtr ToCallbackPtr<TCallback>(ICallbackable callback) where TCallback : ICallbackable =>
        callback switch
        {
            null => IntPtr.Zero,
            CppObject cpp => cpp.NativePointer,
            CallbackBase managed => managed.Find<TCallback>()
        };

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.CppObject"/> instance.
    /// </summary>
    /// <typeparam name="TCallback">The type of the callback.</typeparam>
    /// <param name="obj">The object.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    /// <remarks>This method is meant as a fast-path for codegen to use to reduce the number of casts.</remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static IntPtr ToCallbackPtr<TCallback>(CppObject obj) where TCallback : ICallbackable
        => obj?.NativePointer ?? IntPtr.Zero;

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.CppObject"/> instance.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    /// <remarks>This method is meant as a fast-path for codegen to use to reduce the number of casts.</remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static IntPtr ToCallbackPtr(CppObject obj) => obj?.NativePointer ?? IntPtr.Zero;
}