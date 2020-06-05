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

namespace SharpGen.Runtime
{
    /// <summary>
    /// Helper class providing methods for marshalling interfaces/classes between managed and unmanaged code
    /// </summary>
    public static class MarshallingHelpers
    {
        /// <summary>
        /// Instantiate a CppObject from a native pointer.
        /// </summary>
        /// <typeparam name="T">The CppObject class that will be returned</typeparam>
        /// <param name="cppObjectPtr">The native pointer to a com object.</param>
        /// <returns>An instance of T binded to the native pointer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FromPointer<T>(IntPtr comObjectPtr) where T : CppObject
        {
            return (comObjectPtr == IntPtr.Zero) ? null : (T) Activator.CreateInstance(typeof (T), comObjectPtr);
        }

        /// <summary>
        /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.ICallbackable"/> instance.
        /// </summary>
        /// <typeparam name="TCallback">The type of the callback.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ToCallbackPtr<TCallback>(ICallbackable callback)
            where TCallback : ICallbackable
        {
            // If callback is null, then return a null pointer
            if (callback == null)
                return IntPtr.Zero;

            // If callback is CppObject
            if (callback is CppObject cpp)
                return cpp.NativePointer;

            // Setup the shadow container in order to support multiple inheritance
            var shadowContainer = callback.Shadow;
            if (shadowContainer == null)
            {
                callback.Shadow = new ShadowContainer(callback);
                shadowContainer = callback.Shadow;
            }

            return shadowContainer.Find(typeof(TCallback));
        }

        /// <summary>
        /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.CppObject"/> instance.
        /// </summary>
        /// <typeparam name="TCallback">The type of the callback.</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
        /// <remarks>This method is meant as a fast-path for codegen to use to reduce the number of casts.</remarks>
        public static IntPtr ToCallbackPtr<TCallback>(CppObject obj)
            where TCallback : ICallbackable 
            => obj?.NativePointer ?? IntPtr.Zero;

        /// <summary>
        /// Makes additional transformation for the object which was unmarshalled from unmanaged code 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isOutParam">Indicates that the object was obtained via 'Obj**' parameter</param>
        /// <returns>Transformed object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TransformObjectFromUnmanaged<T>(T obj, bool isOutParam) where T : CppObject
        {
            return obj;
        }
    }
}