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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime;

/// <summary>
/// Utility class.
/// </summary>
public static unsafe partial class MemoryHelpers
{
#pragma warning disable CS8500
    /// <inheritdoc cref="Unsafe.SizeOf{T}" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint SizeOf<T>() => unchecked((uint) sizeof(T));
#pragma warning restore CS8500


    /// <inheritdoc cref="Unsafe.As{T}(object)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(o))]
    public static T? As<T>(this object? o)
        where T : class?
    {
        Debug.Assert(o is null or T);
        return Unsafe.As<T>(o);
    }

    /// <inheritdoc cref="Unsafe.As{TFrom, TTo}(ref TFrom)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo As<TFrom, TTo>(ref TFrom source)
        => ref Unsafe.As<TFrom, TTo>(ref source);

    /// <inheritdoc cref="Unsafe.As{TFrom, TTo}(ref TFrom)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TTo> As<TFrom, TTo>(this Span<TFrom> span)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());

#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
#else
        return new(Unsafe.AsPointer(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span))), span.Length);
#endif
    }

    /// <inheritdoc cref="Unsafe.As{TFrom, TTo}(ref TFrom)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<TTo> As<TFrom, TTo>(this ReadOnlySpan<TFrom> span)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());


#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
#else
        return new(Unsafe.AsPointer(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span))), span.Length);
#endif
    }

    /// <inheritdoc cref="Unsafe.AsPointer{T}(ref T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AsPointer<T>(in T source)
        where T : unmanaged => (T*) Unsafe.AsPointer(ref Unsafe.AsRef(in source));

    /// <inheritdoc cref="Unsafe.As{TFrom, TTo}(ref TFrom)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTo AsReadonly<TFrom, TTo>(in TFrom source)
        => ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in source));

    /// <summary>Reinterprets the given native integer as a reference.</summary>
    /// <typeparam name="T">The type of the reference.</typeparam>
    /// <param name="source">The native integer to reinterpret.</param>
    /// <returns>A reference to a value of type <typeparamref name="T" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(nint source) => ref Unsafe.AsRef<T>((void*) source);

    /// <summary>Reinterprets the given native unsigned integer as a reference.</summary>
    /// <typeparam name="T">The type of the reference.</typeparam>
    /// <param name="source">The native unsigned integer to reinterpret.</param>
    /// <returns>A reference to a value of type <typeparamref name="T" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(nuint source) => ref Unsafe.AsRef<T>((void*) source);

    /// <inheritdoc cref="Unsafe.AsRef{T}(in T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(in T source) => ref Unsafe.AsRef(in source);

    /// <inheritdoc cref="Unsafe.AsRef{T}(in T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo AsRef<TFrom, TTo>(in TFrom source) => ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in source));

    /// <summary>Reinterprets the readonly span as a writeable span.</summary>
    /// <typeparam name="T">The type of items in <paramref name="span" /></typeparam>
    /// <param name="span">The readonly span to reinterpret.</param>
    /// <returns>A writeable span that points to the same items as <paramref name="span" />.</returns>
    public static Span<T> AsSpan<T>(this ReadOnlySpan<T> span)
    {
#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in MemoryMarshal.GetReference(span)), span.Length);
#else
        return new(Unsafe.AsPointer(ref Unsafe.AsRef(in MemoryMarshal.GetReference(span))), span.Length);
#endif
    }

    /// <inheritdoc cref="MemoryMarshal.Cast{TFrom, TTo}(Span{TFrom})" />
    public static Span<TTo> Cast<TFrom, TTo>(this Span<TFrom> span)
        where TFrom : struct
        where TTo : struct => MemoryMarshal.Cast<TFrom, TTo>(span);

    /// <inheritdoc cref="MemoryMarshal.Cast{TFrom, TTo}(ReadOnlySpan{TFrom})" />
    public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(this ReadOnlySpan<TFrom> span)
        where TFrom : struct
        where TTo : struct => MemoryMarshal.Cast<TFrom, TTo>(span);

    // <inheritdoc cref="Unsafe.CopyBlock(ref byte, ref byte, uint)" />
    public static void CopyBlock<TDestination, TSource>(ref TDestination destination, in TSource source, uint byteCount) => Unsafe.CopyBlock(ref Unsafe.As<TDestination, byte>(ref destination), ref Unsafe.As<TSource, byte>(ref Unsafe.AsRef(in source)), byteCount);

    /// <inheritdoc cref="Unsafe.CopyBlockUnaligned(ref byte, ref byte, uint)" />
    public static void CopyBlockUnaligned<TDestination, TSource>(ref TDestination destination, in TSource source, uint byteCount) => Unsafe.CopyBlockUnaligned(ref Unsafe.As<TDestination, byte>(ref destination), ref Unsafe.As<TSource, byte>(ref Unsafe.AsRef(in source)), byteCount);

    /// <inheritdoc cref="MemoryMarshal.CreateSpan{T}(ref T, int)" />
    public static Span<T> CreateSpan<T>(scoped ref T reference, int length)
    {
#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateSpan(ref reference, length);
#else
        return new(Unsafe.AsPointer(ref reference), length);
#endif
    }

    /// <inheritdoc cref="MemoryMarshal.CreateReadOnlySpan{T}(ref T, int)" />
    public static ReadOnlySpan<T> CreateReadOnlySpan<T>(scoped in T reference, int length)
    {
#if NET6_0_OR_GREATER
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in reference), length);
#else
        return new(Unsafe.AsPointer(ref Unsafe.AsRef(in reference)), length);
#endif
    }

    /// <summary>Returns a pointer to the element of the span at index zero.</summary>
    /// <typeparam name="T">The type of items in <paramref name="span" />.</typeparam>
    /// <param name="span">The span from which the pointer is retrieved.</param>
    /// <returns>A pointer to the item at index zero of <paramref name="span" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* GetPointerUnsafe<T>(this Span<T> span)
        where T : unmanaged => (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

    /// <summary>Returns a pointer to the element of the span at index zero.</summary>
    /// <typeparam name="T">The type of items in <paramref name="span" />.</typeparam>
    /// <param name="span">The span from which the pointer is retrieved.</param>
    /// <returns>A pointer to the item at index zero of <paramref name="span" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* GetPointerUnsafe<T>(this ReadOnlySpan<T> span)
        where T : unmanaged => (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

    /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this T[] array) => ref GetArrayDataReference(array);

    /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this T[] array, int index) => ref Unsafe.Add(ref GetArrayDataReference(array), index);

    /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this T[] array, nuint index) => ref Unsafe.Add(ref GetArrayDataReference(array), index);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this Span<T> span) => ref MemoryMarshal.GetReference(span);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this Span<T> span, int index) => ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceUnsafe<T>(this Span<T> span, nuint index) => ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetReferenceUnsafe<T>(this ReadOnlySpan<T> span) => ref MemoryMarshal.GetReference(span);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetReferenceUnsafe<T>(this ReadOnlySpan<T> span, int index) => ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);

    /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetReferenceUnsafe<T>(this ReadOnlySpan<T> span, nuint index) => ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);

    /// <summary>Determines if a given reference to a value of type <typeparamref name="T" /> is not a null reference.</summary>
    /// <typeparam name="T">The type of the reference</typeparam>
    /// <param name="source">The reference to check.</param>
    /// <returns><c>true</c> if <paramref name="source" /> is not a null reference; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNullRef<T>(in T source) => !Unsafe.IsNullRef(ref Unsafe.AsRef(in source));

    /// <inheritdoc cref="Unsafe.IsNullRef{T}(ref T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullRef<T>(in T source) => Unsafe.IsNullRef(ref Unsafe.AsRef(in source));

    /// <inheritdoc cref="Unsafe.NullRef{T}" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T NullRef<T>() => ref Unsafe.NullRef<T>();

    /// <inheritdoc cref="Unsafe.ReadUnaligned{T}(void*)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnaligned<T>(void* source)
        where T : unmanaged => Unsafe.ReadUnaligned<T>(source);

    /// <inheritdoc cref="Unsafe.ReadUnaligned{T}(void*)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnaligned<T>(void* source, nuint offset)
        where T : unmanaged => Unsafe.ReadUnaligned<T>((void*) ((nuint) source + offset));

    /// <inheritdoc cref="Unsafe.WriteUnaligned{T}(void*, T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnaligned<T>(void* source, T value)
        where T : unmanaged => Unsafe.WriteUnaligned(source, value);

    /// <inheritdoc cref="Unsafe.WriteUnaligned{T}(void*, T)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnaligned<T>(void* source, nuint offset, T value)
        where T : unmanaged => Unsafe.WriteUnaligned((void*) ((nuint) source + offset), value);

    /// <summary>
    /// Returns a reference to the 0th element of <paramref name="array"/>. If the array is empty, returns a reference
    /// to where the 0th element would have been stored. Such a reference may be used for pinning but must never be dereferenced.
    /// </summary>
    /// <exception cref="NullReferenceException"><paramref name="array"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetArrayDataReference<T>(T[] array)
    {
#if NET6_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory(IntPtr dest, IntPtr src, int sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned((void*) dest, (void*) src, (uint) sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory(IntPtr dest, IntPtr src, uint sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned((void*) dest, (void*) src, sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory<T>(IntPtr dest, ReadOnlySpan<T> src) where T : struct =>
        src.CopyTo(new Span<T>((void*) dest, src.Length));

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory<T>(IntPtr dest, Span<T> src) where T : struct =>
        src.CopyTo(new Span<T>(dest.ToPointer(), src.Length));

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory(void* dest, void* src, int sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned(dest, src, (uint) sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    /// <param name="sizeInBytesToCopy">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory(void* dest, void* src, uint sizeInBytesToCopy) =>
        Unsafe.CopyBlockUnaligned(dest, src, sizeInBytesToCopy);

    /// <summary>
    /// Native memcpy.
    /// </summary>
    /// <param name="dest">The destination memory location.</param>
    /// <param name="src">The source memory location.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void CopyMemory<T>(void* dest, ReadOnlySpan<T> src) where T : struct =>
        src.CopyTo(new Span<T>(dest, src.Length));

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="value">The value to initialize the block to.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void ClearMemory(IntPtr dest, byte value, int sizeInBytesToClear) =>
        Unsafe.InitBlockUnaligned(ref *(byte*) dest, value, (uint) sizeInBytesToClear);

    /// <summary>
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="value">The value to initialize the block to.</param>
    /// <param name="sizeInBytesToClear">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void ClearMemory(IntPtr dest, byte value, uint sizeInBytesToClear) =>
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
    /// Clears the memory.
    /// </summary>
    /// <param name="dest">The address of the start of the memory block to initialize.</param>
    /// <param name="value">The value to initialize the block to.</param>
    /// <param name="sizeInBytes">The byte count.</param>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static void ClearMemory(void* dest, nuint sizeInBytes) =>
        Unsafe.InitBlockUnaligned(dest, 0, (uint) sizeInBytes);

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
    public static IntPtr Read(IntPtr source, void* data, int sizeInBytes)
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
    public static IntPtr Read<T>(IntPtr source, ReadOnlySpan<T> data, int count) where T : unmanaged
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
    public static IntPtr Write<T>(IntPtr destination, Span<T> data, int count) where T : unmanaged
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
    public static IntPtr Read<T>(IntPtr source, ref T data) where T : unmanaged
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
    public static T Read<T>(IntPtr source) where T : unmanaged
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
    public static IntPtr Write<T>(IntPtr destination, ref T data) where T : unmanaged
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
    public static IntPtr Write<T>(IntPtr destination, T data) where T : unmanaged =>
        Write(destination, &data, sizeof(T));

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static bool IsMemoryAligned(nint memoryPtr, uint alignment = 16) =>
        (memoryPtr & (alignment - 1)) == 0;

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static bool IsMemoryAligned(nuint memoryPtr, uint alignment = 16) =>
        (memoryPtr & (alignment - 1)) == 0;

    /// <summary>
    /// Determines whether the specified memory pointer is aligned in memory.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="align">The align.</param>
    /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
    [MethodImpl(Utilities.MethodAggressiveOptimization)]
    public static bool IsMemoryAligned(void* memoryPtr, uint alignment = 16) =>
        IsMemoryAligned((nuint) memoryPtr, alignment);

#nullable enable

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

    public static void Dispose<T>(ref T? value, bool disposing = true) where T : class
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

    public static void Dispose<T>(T? value, bool disposing = true) where T : struct
    {
        switch (value)
        {
            case IEnlightenedDisposable disposable:
                disposable.CheckAndDispose(disposing);
                return;
            case IDisposable disposable:
                if (disposing)
                    disposable.Dispose();
                return;
        }
    }

    public static void Dispose<T>(T? value, bool disposing = true) where T : class
    {
        switch (value)
        {
            case IEnlightenedDisposable disposable:
                disposable.CheckAndDispose(disposing);
                return;
            case IDisposable disposable:
                if (disposing)
                    disposable.Dispose();
                return;
        }
    }
}