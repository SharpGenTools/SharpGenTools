using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

public static partial class MemoryHelpers
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
    public static unsafe void* AllocateMemory(nuint sizeInBytes, uint align = 16)
    {
        nuint mask = align - 1u;
        var memPtr = Marshal.AllocHGlobal((nint) (sizeInBytes + mask) + sizeof(void*));
        var ptr = (nuint) ((byte*) memPtr + sizeof(void*) + mask) & ~mask;
        Debug.Assert(IsMemoryAligned(ptr, align));
        ((IntPtr*) ptr)[-1] = memPtr;
        return (void*) ptr;
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
    public static unsafe IntPtr AllocateMemory(int sizeInBytes, int align = 16) =>
        new(AllocateMemory((nuint) sizeInBytes, (uint) align));

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    public static unsafe void FreeMemory(void* alignedBuffer)
    {
        if (alignedBuffer == default) return;

        Marshal.FreeHGlobal(((IntPtr*) alignedBuffer)[-1]);
    }

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void FreeMemory(UIntPtr alignedBuffer) => FreeMemory(alignedBuffer.ToPointer());

    /// <summary>
    /// Free an aligned memory buffer.
    /// </summary>
    /// <returns>A pointer to a buffer aligned.</returns>
    /// <remarks>
    /// The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void FreeMemory(IntPtr alignedBuffer) => FreeMemory(alignedBuffer.ToPointer());
}