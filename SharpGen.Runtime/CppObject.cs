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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpGen.Runtime.Diagnostics;

namespace SharpGen.Runtime;

/// <summary>
/// Root class for all native interop objects.
/// </summary>
public class CppObject : DisposeBase, ICallbackable, IEquatable<CppObject>
{
    /// <summary>
    /// The native pointer
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private IntPtr _nativePointer;

    private static readonly ConditionalWeakTable<CppObject, object> TagTable = new();

    /// <summary>
    /// Gets or sets a custom user tag object to associate with this instance.
    /// </summary>
    /// <value>The tag object.</value>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public object Tag
    {
        get => TagTable.TryGetValue(this, out var tag) ? tag : null;
        set => TagTable.Add(this, value);
    }

    /// <summary>
    ///   Default constructor.
    /// </summary>
    /// <param name = "pointer">Pointer to C++ object</param>
    public CppObject(IntPtr pointer)
    {
        _nativePointer = pointer;
        if (ObjectTrackerReadOnlyConfiguration.IsEnabled)
            ObjectTracker.Track(this, pointer);
    }

    /// <summary>
    ///   Default constructor.
    /// </summary>
    /// <param name = "pointer">Pointer to C++ object</param>
    public unsafe CppObject(UIntPtr pointer) : this(new IntPtr(pointer.ToPointer()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CppObject"/> class.
    /// </summary>
    protected CppObject()
    {
    }

    /// <summary>
    ///   Get a pointer to the underlying Cpp Object
    /// </summary>
    public IntPtr NativePointer
    {
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
        get => _nativePointer;
        set
        {
            var oldNativePointer = Interlocked.Exchange(ref _nativePointer, value);
            if (oldNativePointer == value)
                return;

            if (ObjectTrackerReadOnlyConfiguration.IsEnabled)
                ObjectTracker.MigrateNativePointer(this, oldNativePointer, value);

            NativePointerUpdated(oldNativePointer);
        }
    }

    public override string ToString() => Utilities.FormatPointer(NativePointer);

    public static explicit operator IntPtr(CppObject cppObject) => cppObject?.NativePointer ?? IntPtr.Zero;

    /// <summary>
    /// Method called when the <see cref="NativePointer"/> is updated.
    /// </summary>
    protected virtual void NativePointerUpdated(IntPtr oldNativePointer)
    {
    }

    protected unsafe void* this[int index]
    {
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
#if DEBUG
        get
        {
            Debug.Assert(!IsDisposed);
            return (*(void***) _nativePointer)[index];
        }
#else
            get => (*(void***) _nativePointer)[index];
#endif
    }

    protected unsafe void* this[uint index]
    {
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
#if DEBUG
        get
        {
            Debug.Assert(!IsDisposed);
            return (*(void***) _nativePointer)[index];
        }
#else
            get => (*(void***) _nativePointer)[index];
#endif
    }

    protected unsafe void* this[nint index]
    {
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
#if DEBUG
        get
        {
            Debug.Assert(!IsDisposed);
            return (*(void***) _nativePointer)[index];
        }
#else
            get => (*(void***) _nativePointer)[index];
#endif
    }

    protected unsafe void* this[nuint index]
    {
        [MethodImpl(Utilities.MethodAggressiveOptimization)]
#if DEBUG
        get
        {
            Debug.Assert(!IsDisposed);
            return (*(void***) _nativePointer)[index];
        }
#else
            get => (*(void***) _nativePointer)[index];
#endif
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override bool IsDisposed => NativePointer == IntPtr.Zero;

    protected sealed override void Dispose(bool disposing)
    {
        var nativePointer = NativePointer;

        DisposeCore(nativePointer, disposing);

        if (ObjectTrackerReadOnlyConfiguration.IsEnabled)
            ObjectTracker.Untrack(this, nativePointer);

        // Set pointer to null (using protected members in order to avoid callbacks).
#if DEBUG
        var oldNativePointer = Interlocked.Exchange(ref _nativePointer, IntPtr.Zero);
        Debug.Assert(oldNativePointer == nativePointer);
#else
            Interlocked.Exchange(ref _nativePointer, IntPtr.Zero);
#endif

        NativePointerUpdated(nativePointer);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources
    /// </summary>
    /// <param name="nativePointer"><see cref="NativePointer"/></param>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void DisposeCore(IntPtr nativePointer, bool disposing)
    {
    }

    public bool Equals(CppObject other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || _nativePointer.Equals(other._nativePointer);
    }

    public override bool Equals(object obj) =>
        ReferenceEquals(this, obj) || obj is CppObject other && _nativePointer.Equals(other._nativePointer);

    public override int GetHashCode() => _nativePointer.GetHashCode();
    public static bool operator ==(CppObject left, CppObject right) => Equals(left, right);
    public static bool operator !=(CppObject left, CppObject right) => !Equals(left, right);
}