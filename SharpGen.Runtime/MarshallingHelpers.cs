using System;

namespace SharpGen.Runtime
{
    public static class MarshallingHelpers
    {
        /// <summary>
        /// Instantiate a CppObject from a native pointer.
        /// </summary>
        /// <typeparam name="T">The CppObject class that will be returned</typeparam>
        /// <param name="cppObjectPtr">The native pointer to a com object.</param>
        /// <returns>An instance of T bound to the native pointer</returns>
        public static T FromPointer<T>(IntPtr cppObjectPtr) where T : CppObject =>
            cppObjectPtr == IntPtr.Zero ? null : (T) Activator.CreateInstance(typeof(T), cppObjectPtr);

        /// <summary>
        /// Return the unmanaged C++ pointer from a <see cref="SharpGen.Runtime.ICallbackable"/> instance.
        /// </summary>
        /// <typeparam name="TCallback">The type of the callback.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <returns>A pointer to the unmanaged C++ object of the callback</returns>
        public static IntPtr ToCallbackPtr<TCallback>(ICallbackable callback) where TCallback : ICallbackable
        {
            switch (callback)
            {
                case null:
                    return IntPtr.Zero;
                case CppObject cpp:
                    return cpp.NativePointer;
            }

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
        public static IntPtr ToCallbackPtr<TCallback>(CppObject obj) where TCallback : ICallbackable
            => obj?.NativePointer ?? IntPtr.Zero;
    }
}