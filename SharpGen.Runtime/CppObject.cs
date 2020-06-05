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
using SharpGen.Runtime.Diagnostics;
using System;

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
        public static Action<string> LogMemoryLeakWarning = (warning) => System.Diagnostics.Debug.WriteLine(warning);

        /// <summary>
        /// The native pointer
        /// </summary>
        protected unsafe void* _nativePointer;

        /// <summary>
        /// Gets or sets a custom user tag object to associate with this instance..
        /// </summary>
        /// <value>The tag object.</value>
        public object Tag { get; set; }

        /// <summary>
        ///   Default constructor.
        /// </summary>
        /// <param name = "pointer">Pointer to Cpp Object</param>
        public CppObject(IntPtr pointer)
        {
            NativePointer = pointer;
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
            get
            {
                unsafe
                {
                    return (IntPtr) _nativePointer;
                }
            }
            set
            {
                unsafe
                {
                    var newNativePointer = (void*) value;
                    if (_nativePointer != newNativePointer)
                    {
                        NativePointerUpdating();
                        var oldNativePointer = _nativePointer;
                        _nativePointer = newNativePointer;
                        NativePointerUpdated((IntPtr)oldNativePointer);
                    }
                }
            }
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="SharpDX.CppObject"/> to <see cref="System.IntPtr"/>.
        /// </summary>
        /// <param name="cppObject">The CPP object.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator IntPtr(CppObject cppObject)
        {
            return cppObject == null ? IntPtr.Zero : cppObject.NativePointer;
        }

        /// <summary>
        /// Method called when <see cref="NativePointer"/> is going to be update.
        /// </summary>
        protected virtual void NativePointerUpdating()
        {
            if (Configuration.EnableObjectTracking)
                ObjectTracker.UnTrack(this);
        }

        /// <summary>
        /// Method called when the <see cref="NativePointer"/> is updated.
        /// </summary>
        protected virtual void NativePointerUpdated(IntPtr oldNativePointer)
        {
            if (Configuration.EnableObjectTracking)
                ObjectTracker.Track(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (NativePointer != IntPtr.Zero)
            {
                // If object is disposed by the finalizer, emits a warning
                if(!disposing && Configuration.EnableTrackingReleaseOnFinalizer)
                {
                    if(!Configuration.EnableReleaseOnFinalizer)
                    {
                        var objectReference = ObjectTracker.Find(this);
                        LogMemoryLeakWarning?.Invoke(string.Format("Warning: Live CppObject released on finalizer [0x{0:X}], potential memory leak: {1}", NativePointer.ToInt64(), objectReference));
                    }
                }

                if (Configuration.EnableObjectTracking)
                {
                    ObjectTracker.UnTrack(this);
                }

                unsafe
                {
                    // Set pointer to null (using protected members in order to avoid callbacks.
                    _nativePointer = (void*)0;
                }
            }
        }

        /// <summary>
        /// Implements <see cref="ICallbackable"/> but it cannot not be set. 
        /// This is only used to support for interop with unmanaged callback.
        /// </summary>
        ShadowContainer ICallbackable.Shadow
        {
            get { throw new InvalidOperationException("Invalid access to Callback. This is used internally."); }
            set { throw new InvalidOperationException("Invalid access to Callback. This is used internally."); }
        }
    }
}