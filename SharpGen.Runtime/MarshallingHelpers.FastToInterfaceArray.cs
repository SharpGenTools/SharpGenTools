using System;

namespace SharpGen.Runtime
{
    public static partial class MarshallingHelpers
    {
        /// <summary>
        /// Converts an array of native object pointers to a <see cref="CppObject"/> array.
        /// </summary>
        public static void ConvertToInterfaceArrayFast<TCallback>(ReadOnlySpan<IntPtr> pointers,
                                                                  Span<TCallback> interfaces)
            where TCallback : CppObject
        {
            var arrayLength = pointers.Length;
            for (var i = 0; i < arrayLength; ++i)
                interfaces[i].NativePointer = pointers[i];
        }

        /// <summary>
        /// Converts an array of native object pointers to a <see cref="CppObject"/> array.
        /// </summary>
        public static void ConvertToInterfaceArrayFast<TCallback>(Span<IntPtr> pointers, Span<TCallback> interfaces)
            where TCallback : CppObject
        {
            var arrayLength = pointers.Length;
            for (var i = 0; i < arrayLength; ++i)
                interfaces[i].NativePointer = pointers[i];
        }

        /// <summary>
        /// Converts an array of native object pointers to a <see cref="CppObject"/> array.
        /// </summary>
        public static void ConvertToInterfaceArrayFast<TCallback>(Span<IntPtr> pointers, TCallback[] interfaces)
            where TCallback : CppObject
        {
            var arrayLength = pointers.Length;
            for (var i = 0; i < arrayLength; ++i)
                interfaces[i].NativePointer = pointers[i];
        }

        /// <summary>
        /// Converts an array of native object pointers to a <see cref="CppObject"/> array.
        /// </summary>
        public static void ConvertToInterfaceArrayFast<TCallback>(Span<IntPtr> pointers, CppObject[] interfaces)
            where TCallback : CppObject
        {
            var arrayLength = pointers.Length;
            for (var i = 0; i < arrayLength; ++i)
                interfaces[i].NativePointer = pointers[i];
        }

        /// <summary>
        /// Converts an array of native object pointers to a <see cref="CppObject"/> array.
        /// </summary>
        public static void ConvertToInterfaceArrayFast(Span<IntPtr> pointers, CppObject[] interfaces)
        {
            var arrayLength = pointers.Length;
            for (var i = 0; i < arrayLength; ++i)
                interfaces[i].NativePointer = pointers[i];
        }
    }
}