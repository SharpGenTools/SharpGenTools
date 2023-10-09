using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace SharpGen.Runtime;

public static unsafe partial class MemoryHelpers
{
    /// <summary>
    /// Allocate an aligned memory buffer.
    /// </summary>
    /// <param name="sizeInBytes">Size of the buffer to allocate.</param>
    /// <param name="align">Alignment, 16 bytes by default.</param>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// To free this buffer, call <see cref="FreeMemory(void*)"/>.
    /// </remarks>
    public static void* AllocateMemory(nuint sizeInBytes, uint alignment = 16)
    {
#if NET6_0_OR_GREATER
        var ptr = NativeMemory.AlignedAlloc(sizeInBytes, alignment);
        Debug.Assert(IsMemoryAligned(ptr, alignment));
        return ptr;
#else
        nuint mask = alignment - 1u;
        var memPtr = Marshal.AllocHGlobal((nint) (sizeInBytes + mask) + sizeof(void*));
        var ptr = (nuint) ((byte*) memPtr + sizeof(void*) + mask) & ~mask;
        Debug.Assert(IsMemoryAligned(ptr, alignment));
        ((IntPtr*) ptr)[-1] = memPtr;
        return (void*) ptr;
#endif
    }

    /// <summary>
    /// Allocate an aligned memory buffer.
    /// </summary>
    /// <param name="sizeInBytes">Size of the buffer to allocate.</param>
    /// <param name="align">Alignment, 16 bytes by default.</param>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// To free this buffer, call <see cref="FreeMemory(IntPtr)"/>.
    /// </remarks>
    [Obsolete("Use void*(nuint, uint) overload instead")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static IntPtr AllocateMemory(int sizeInBytes, int align = 16) =>
        new(AllocateMemory((nuint) sizeInBytes, (uint) align));

    /// <summary>Allocates a chunk of unmanaged memory.</summary>
    /// <param name="count">The count of elements contained in the allocation.</param>
    /// <param name="size">The size, in bytes, of the elements in the allocation.</param>
    /// <param name="zero"><c>true</c> if the allocated memory should be zeroed; otherwise, <c>false</c>.</param>
    /// <returns>The address to an allocated chunk of memory that is at least <paramref name="size" /> bytes in length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AllocateArray(nuint count, nuint size, bool zero = false)
    {
#if NET6_0_OR_GREATER
        void* result = NativeMemory.Alloc(count, size);
        
#else
        void* result = (void*)Marshal.AllocHGlobal(checked((int) (count * size)));
#endif

        if (result == null)
        {
            ThrowOutOfMemoryException(count, size);
        }

        if(zero)
        {
#if NET6_0_OR_GREATER
            NativeMemory.Clear(result, count * size);
#else
            ClearMemory(result, count * size);
#endif
        }

        return result;
    }

    /// <summary>Allocates a chunk of unmanaged memory.</summary>
    /// <typeparam name="T">The type used to compute the size, in bytes, of the elements in the allocation.</typeparam>
    /// <param name="count">The count of elements contained in the allocation.</param>
    /// <param name="zero"><c>true</c> if the allocated memory should be zeroed; otherwise, <c>false</c>.</param>
    /// <returns>The address to an allocated chunk of memory that is at least <c>sizeof(<typeparamref name="T" />)</c> bytes in length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AllocateArray<T>(nuint count, bool zero = false)
        where T : unmanaged
    {

#if NET6_0_OR_GREATER
        T* result = (T*)NativeMemory.Alloc(count, SizeOf<T>());
        
#else
        T* result = (T*) Marshal.AllocHGlobal(checked((int) (count * SizeOf<T>())));
#endif

        if (result == null)
        {
            ThrowOutOfMemoryException(count, SizeOf<T>());
        }

        if (zero)
        {
#if NET6_0_OR_GREATER
            NativeMemory.Clear(result, count * SizeOf<T>());
#else
            ClearMemory(result, count * SizeOf<T>());
#endif
        }

        return result;
    }

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    public static void FreeMemory(void* alignedBuffer)
    {
        if (alignedBuffer == default) return;

#if NET6_0_OR_GREATER
        NativeMemory.AlignedFree(alignedBuffer);
#else
        Marshal.FreeHGlobal(((IntPtr*) alignedBuffer)[-1]);
#endif
    }

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void FreeMemory(UIntPtr alignedBuffer) => FreeMemory(alignedBuffer.ToPointer());

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void FreeMemory(IntPtr alignedBuffer) => FreeMemory(alignedBuffer.ToPointer());

    [DoesNotReturn]
    private static void ThrowOutOfMemoryException(ulong size)
    {
        throw new OutOfMemoryException($"The allocation of '{size}' bytes failed");
    }

    [DoesNotReturn]
    public static void ThrowOutOfMemoryException(ulong count, ulong size)
    {
        throw new OutOfMemoryException($"The allocation of '{count}x{size}' bytes failed");
    }
}