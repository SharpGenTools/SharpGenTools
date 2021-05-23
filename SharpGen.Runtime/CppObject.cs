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

namespace SharpGen.Runtime
{
    /// <summary>
    /// Root class for all Cpp interop object.
    /// </summary>
    public class CppObject : DisposeBase, ICallbackable
    {
        /// <summary>
        /// Logs a warning of a possible memory leak when <see cref="Configuration.EnableObjectTracking" /> is enabled.
        /// Default uses <see cref="System.Diagnostics.Debug"/>.
        /// </summary>
        public static Action<string> LogMemoryLeakWarning = DefaultMemoryLeakWarningLogger;

        private static void DefaultMemoryLeakWarningLogger(string warning)
        {
            Debug.WriteLine(warning);
        }

        /// <summary>
        /// The native pointer
        /// </summary>
        private IntPtr _nativePointer;

#if !NETSTANDARD1_1
        private static readonly ConditionalWeakTable<CppObject, object> TagTable = new();
#else
        private object tag;
#endif

        /// <summary>
        /// Gets or sets a custom user tag object to associate with this instance.
        /// </summary>
        /// <value>The tag object.</value>
        public object Tag
        {
#if !NETSTANDARD1_1
            get => TagTable.TryGetValue(this, out var tag) ? tag : null;
            set => TagTable.Add(this, value);
#else
            get => tag;
            set => tag = value;
#endif
        }

        /// <summary>
        ///   Default constructor.
        /// </summary>
        /// <param name = "pointer">Pointer to Cpp Object</param>
        public CppObject(IntPtr pointer)
        {
            NativePointer = pointer;
#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine($"{GetType().Name}[{NativePointer.ToInt64():X}]::new()");
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CppObject"/> class.
        /// </summary>
        protected CppObject()
        {
#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine($"{GetType().Name}[0]::new()");
#endif
        }

        /// <summary>
        ///   Get a pointer to the underlying Cpp Object
        /// </summary>
        public IntPtr NativePointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !NETSTANDARD1_1
            get => _nativePointer;
#else
            get => Volatile.Read(ref _nativePointer);
#endif
            set
            {
                var oldNativePointer = Interlocked.Exchange(ref _nativePointer, value);
                if (oldNativePointer != value)
                    NativePointerUpdated(oldNativePointer);
            }
        }

        public static explicit operator IntPtr(CppObject cppObject) => cppObject?.NativePointer ?? IntPtr.Zero;

        /// <summary>
        /// Method called when the <see cref="NativePointer"/> is updated.
        /// </summary>
        protected virtual void NativePointerUpdated(IntPtr oldNativePointer)
        {
            if (!Configuration.EnableObjectTracking)
                return;

#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine(
                    $"{GetType().Name}[{oldNativePointer.ToInt64():X}]::{nameof(NativePointerUpdated)} ({NativePointer.ToInt64():X})"
                );
#endif

            ObjectTracker.MigrateNativePointer(this, oldNativePointer, NativePointer);
        }

        protected unsafe void* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (*(void***) _nativePointer)[index];
        }

        protected unsafe void* this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (*(void***) _nativePointer)[index];
        }

        protected unsafe void* this[nint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (*(void***) _nativePointer)[index];
        }

        protected unsafe void* this[nuint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (*(void***) _nativePointer)[index];
        }

        protected sealed override void Dispose(bool disposing)
        {
            var nativePointer = NativePointer;
            if (nativePointer == IntPtr.Zero)
                return;

            var isObjectTrackingEnabled = Configuration.EnableObjectTracking;

            // If object is disposed by the finalizer, emits a warning
            if (!disposing && isObjectTrackingEnabled && Configuration.EnableTrackingReleaseOnFinalizer && !Configuration.EnableReleaseOnFinalizer)
            {
                var objectReference = ObjectTracker.Find(this, nativePointer);
                LogMemoryLeakWarning?.Invoke(
                    $"Warning: Live CppObject released on finalizer [0x{nativePointer.ToInt64():X}], potential memory leak: {objectReference}"
                );
            }

#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine(
                    $"{GetType().Name}[{nativePointer.ToInt64():X}]::{nameof(Dispose)}"
                );
#endif

            DisposeCore(nativePointer, disposing);

            if (isObjectTrackingEnabled)
                ObjectTracker.Untrack(this, nativePointer);

            // Set pointer to null (using protected members in order to avoid callbacks).
            Interlocked.Exchange(ref _nativePointer, IntPtr.Zero);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="nativePointer"><see cref="NativePointer"/></param>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void DisposeCore(IntPtr nativePointer, bool disposing)
        {
        }

        [Obsolete("Use " + nameof(MarshallingHelpers) + "." + nameof(MarshallingHelpers.FromPointer) + " instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T FromPointer<T>(IntPtr cppObjectPtr) where T : CppObject =>
            MarshallingHelpers.FromPointer<T>(cppObjectPtr);

        [Obsolete("Use " + nameof(MarshallingHelpers) + "." + nameof(MarshallingHelpers.ToCallbackPtr) + " instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr ToCallbackPtr<TCallback>(ICallbackable callback) where TCallback : ICallbackable =>
            MarshallingHelpers.ToCallbackPtr<TCallback>(callback);

        [Obsolete("Use " + nameof(MarshallingHelpers) + "." + nameof(MarshallingHelpers.ToCallbackPtr) + " instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr ToCallbackPtr<TCallback>(CppObject obj) where TCallback : ICallbackable =>
            MarshallingHelpers.ToCallbackPtr<TCallback>(obj);

        /// <summary>
        /// Implements <see cref="ICallbackable"/> but it cannot not be set.
        /// This is only used to support for interop with unmanaged callback.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ShadowContainer ICallbackable.Shadow => throw new InvalidOperationException("Invalid access to Callback. This is used internally.");
    }
}