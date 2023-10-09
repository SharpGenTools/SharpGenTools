#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
    public static T? FromPointer<T>(IntPtr cppObjectPtr, Func<IntPtr, T> factory) where T : CppObject
    {
        if (cppObjectPtr == IntPtr.Zero)
            return default;

        T? result = factory(cppObjectPtr);
        return result;
    }

    /// <summary>
    /// Instantiate a CppObject from a native pointer.
    /// </summary>
    /// <typeparam name="T">The CppObject class that will be returned</typeparam>
    /// <param name="cppObjectPtr">The native pointer to a C++ object.</param>
    /// <returns>An instance of T bound to the native pointer</returns>
    public static T? FromPointer<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(IntPtr cppObjectPtr) where T : CppObject
    {
        if (cppObjectPtr == IntPtr.Zero)
            return default;

        object? result = Activator.CreateInstance(typeof(T), cppObjectPtr);
        if (result is null)
            return default;

        return (T) result;
    }

    /// <summary>
    /// Instantiate a CppObject from a native pointer.
    /// </summary>
    /// <typeparam name="T">The CppObject class that will be returned</typeparam>
    /// <param name="cppObjectPtr">The native pointer to a C++ object.</param>
    /// <returns>An instance of T bound to the native pointer</returns>
    public static T? FromPointer<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T>(UIntPtr cppObjectPtr) where T : CppObject
    {
        if (cppObjectPtr == UIntPtr.Zero)
            return default;

        object? result = Activator.CreateInstance(typeof(T), cppObjectPtr);
        if (result is null)
            return default;

        return (T) result;
    }

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static uint AddRef<TCallback>(TCallback callback) where TCallback : ICallbackable =>
        callback switch
        {
            ComObject cpp => cpp.AddRef(),
            CallbackBase managed => managed.AddRef(),
            _ => throw new NotImplementedException(),
        };

    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static uint Release<TCallback>(TCallback callback) where TCallback : ICallbackable =>
        callback switch
        {
            ComObject cpp => cpp.Release(),
            CallbackBase managed => managed.Release(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.ICallbackable"/> instance.
    /// </summary>
    /// <typeparam name="TCallback">The type of the callback.</typeparam>
    /// <param name="callback">The callback.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    public static nint ToCallbackPtr<TCallback>(ICallbackable callback) where TCallback : ICallbackable =>
        callback switch
        {
            CppObject cpp => cpp.NativePointer,
            CallbackBase managed => managed.Find<TCallback>(),
            _ => 0,
        };

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.CppObject"/> instance.
    /// </summary>
    /// <typeparam name="TCallback">The type of the callback.</typeparam>
    /// <param name="obj">The object.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    /// <remarks>This method is meant as a fast-path for codegen to use to reduce the number of casts.</remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static IntPtr ToCallbackPtr<TCallback>(CppObject? obj) where TCallback : ICallbackable
        => obj?.NativePointer ?? IntPtr.Zero;

    /// <summary>
    /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.CppObject"/> instance.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
    /// <remarks>This method is meant as a fast-path for codegen to use to reduce the number of casts.</remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static IntPtr ToCallbackPtr(CppObject? obj) => obj?.NativePointer ?? IntPtr.Zero;
}