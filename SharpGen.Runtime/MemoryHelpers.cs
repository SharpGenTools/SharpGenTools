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

using System;
using System.Runtime.CompilerServices;

namespace SharpGen.Runtime;

/// <summary>
/// Utility class.
/// </summary>
public static partial class MemoryHelpers
{
    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory(IntPtr dest, IntPtr src, int sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned((void*) dest, (void*) src, (uint) sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory(IntPtr dest, IntPtr src, uint sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned((void*) dest, (void*) src, sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory<T>(IntPtr dest, ReadOnlySpan<T> src) where T : struct =>
        src.CopyTo(new Span<T>((void*) dest, src.Length));

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory(void* dest, void* src, int sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned(dest, src, (uint) sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory(void* dest, void* src, uint sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned(dest, src, sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void CopyMemory<T>(void* dest, ReadOnlySpan<T> src) where T : struct =>
        src.CopyTo(new Span<T>(dest, src.Length));

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="value">The value to initialize the block to.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void ClearMemory(IntPtr dest, byte value, int sizeInBytesToClear) =>
        Unsafe.InitBlockUnaligned(ref *(byte*) dest, value, (uint) sizeInBytesToClear);

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="value">The value to initialize the block to.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe void ClearMemory(IntPtr dest, byte value, uint sizeInBytesToClear) =>
        Unsafe.InitBlockUnaligned(ref *(byte*) dest, value, sizeInBytesToClear);

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void ClearMemory(IntPtr dest, int sizeInBytesToClear) =>
        ClearMemory(dest, 0, (uint) sizeInBytesToClear);

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void ClearMemory(IntPtr dest, uint sizeInBytesToClear) =>
        ClearMemory(dest, 0, sizeInBytesToClear);

    /// <summary>
    /// Reads the specified array T[] data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data write to.</param>
    /// <param name="offset">The offset in the array to write to.</param>
    /// <param name="count">The number of T element to read from the memory location.</param>
    /// <returns>source pointer + sizeof(T) * count</returns>
    public static IntPtr Read<T>(IntPtr source, T[] data, int offset, int count) where T : unmanaged =>
        Read(source, new ReadOnlySpan<T>(data).Slice(offset), count);

    /// <summary>
    /// Reads the block of data from a memory location.
    /// </summary>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The target data pointer.</param>
    /// <param name="sizeInBytes">The byte count to read from the memory location.</param>
    /// <returns>source pointer + sizeInBytes</returns>
    public static unsafe IntPtr Read(IntPtr source, void* data, int sizeInBytes)
    {
        Unsafe.CopyBlockUnaligned(data, (void*) source, (uint) sizeInBytes);
        return source + sizeInBytes;
    }

    /// <summary>
    /// Writes the block of data to a memory location.
    /// </summary>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The span of T data to write.</param>
    /// <param name="sizeInBytes">The byte count to write to the memory location.</param>
    /// <returns>destination pointer + sizeInBytes</returns>
    public static unsafe IntPtr Write(IntPtr destination, void* data, int sizeInBytes)
    {
        Unsafe.CopyBlockUnaligned((void*) destination, data, (uint) sizeInBytes);
        return destination + sizeInBytes;
    }

    /// <summary>
    /// Reads the specified array data from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The data span to write to.</param>
    /// <param name="count">The number of T element to read from the memory location.</param>
    /// <returns>source pointer + sizeof(T) * count</returns>
    public static unsafe IntPtr Read<T>(IntPtr source, ReadOnlySpan<T> data, int count) where T : unmanaged
    {
        fixed (void* dataPtr = data)
            return Read(source, dataPtr, count * sizeof(T));
    }

    /// <summary>
    /// Writes the specified array T[] data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The span of T data to write.</param>
    /// <param name="count">The number of T element to write to the memory location.</param>
    /// <returns>destination pointer + sizeof(T) * count</returns>
    public static unsafe IntPtr Write<T>(IntPtr destination, Span<T> data, int count) where T : unmanaged
    {
        fixed (void* dataPtr = data)
            return Write(destination, dataPtr, count * sizeof(T));
    }

    /// <summary>
    /// Reads the data of specified type from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <param name="data">The T to read to.</param>
    /// <returns>source pointer + sizeof(T)</returns>
    public static unsafe IntPtr Read<T>(IntPtr source, ref T data) where T : unmanaged
    {
        fixed (void* dataPtr = &data)
            return Read(source, dataPtr, sizeof(T));
    }

    /// <summary>
    /// Reads the data of specified type from a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to read.</typeparam>
    /// <param name="source">Memory location to read from.</param>
    /// <returns>The T value read from the pointer.</returns>
    public static unsafe T Read<T>(IntPtr source) where T : unmanaged
    {
        T data = default;
        Read(source, &data, sizeof(T));
        return data;
    }

    /// <summary>
    /// Writes the specified array T data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The data structure to write.</param>
    /// <returns>destination pointer + sizeof(T)</returns>
    public static unsafe IntPtr Write<T>(IntPtr destination, ref T data) where T : unmanaged
    {
        fixed (void* dataPtr = &data)
            return Write(destination, dataPtr, sizeof(T));
    }

    /// <summary>
    /// Writes the specified array T data to a memory location.
    /// </summary>
    /// <typeparam name="T">Type of a data to write.</typeparam>
    /// <param name="destination">Memory location to write to.</param>
    /// <param name="data">The data structure to write.</param>
    /// <returns>destination pointer + sizeof(T)</returns>
    public static unsafe IntPtr Write<T>(IntPtr destination, T data) where T : unmanaged =>
        Write(destination, &data, sizeof(T));

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static bool IsMemoryAligned(nint memoryPtr, uint align = 16) =>
        (memoryPtr & (align - 1)) == 0;

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static bool IsMemoryAligned(nuint memoryPtr, uint align = 16) =>
        (memoryPtr & (align - 1)) == 0;

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static unsafe bool IsMemoryAligned(void* memoryPtr, uint align = 16) =>
        IsMemoryAligned((nuint) memoryPtr, align);

    public static void Dispose<T>(ref T? value, bool disposing = true) where T : struct
    {
        switch (value)
        {
            case null:
                return;
            case IEnlightenedDisposable disposable:
                value = null;
                disposable.CheckAndDispose(disposing);
                return;
            case IDisposable disposable:
                value = null;
                if (disposing)
                    disposable.Dispose();
                return;
            default:
                value = null;
                return;
        }
    }

    public static void Dispose<T>(ref T value, bool disposing = true) where T : class
    {
        switch (value)
        {
            case null:
                return;
            case IEnlightenedDisposable disposable:
                value = null;
                disposable.CheckAndDispose(disposing);
                return;
            case IDisposable disposable:
                value = null;
                if (disposing)
                    disposable.Dispose();
                return;
            default:
                value = null;
                return;
        }
    }
}