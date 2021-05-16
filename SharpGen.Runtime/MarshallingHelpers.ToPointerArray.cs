using System;

namespace SharpGen.Runtime
{
    public static partial class MarshallingHelpers
    {
        /// <summary>
        /// Converts an <see cref="ICallbackable"/> array to an array of native object pointers.
        /// </summary>
        public static void ConvertToPointerArray<TCallback>(Span<IntPtr> pointers, ReadOnlySpan<TCallback> interfaces)
            where TCallback : ICallbackable
        {
            var arrayLength = interfaces.Length;
            Guid? callbackTypeGuid = null;

            for (var i = 0; i < arrayLength; ++i)
                pointers[i] = interfaces[i] switch
                {
                    null => IntPtr.Zero,
                    CppObject cpp => cpp.NativePointer,
                    { } item => GetShadowContainer(item).Find(callbackTypeGuid ??= GetCallbackTypeGuid())
                };

            static Guid GetCallbackTypeGuid() => ShadowContainer.GuidFromType(typeof(TCallback));
        }

        /// <summary>
        /// Converts an <see cref="ICallbackable"/> array to an array of native object pointers.
        /// </summary>
        public static void ConvertToPointerArray<TCallback>(Span<IntPtr> pointers, Span<TCallback> interfaces)
            where TCallback : ICallbackable
        {
            var arrayLength = interfaces.Length;
            Guid? callbackTypeGuid = null;

            for (var i = 0; i < arrayLength; ++i)
                pointers[i] = interfaces[i] switch
                {
                    null => IntPtr.Zero,
                    CppObject cpp => cpp.NativePointer,
                    { } item => GetShadowContainer(item).Find(callbackTypeGuid ??= GetCallbackTypeGuid())
                };

            static Guid GetCallbackTypeGuid() => ShadowContainer.GuidFromType(typeof(TCallback));
        }

        /// <summary>
        /// Converts an <see cref="ICallbackable"/> array to an array of native object pointers.
        /// </summary>
        public static void ConvertToPointerArray<TCallback>(Span<IntPtr> pointers, TCallback[] interfaces)
            where TCallback : ICallbackable
        {
            var arrayLength = interfaces.Length;
            Guid? callbackTypeGuid = null;

            for (var i = 0; i < arrayLength; ++i)
                pointers[i] = interfaces[i] switch
                {
                    null => IntPtr.Zero,
                    CppObject cpp => cpp.NativePointer,
                    { } item => GetShadowContainer(item).Find(callbackTypeGuid ??= GetCallbackTypeGuid())
                };

            static Guid GetCallbackTypeGuid() => ShadowContainer.GuidFromType(typeof(TCallback));
        }

        /// <summary>
        /// Converts an <see cref="ICallbackable"/> array to an array of native object pointers.
        /// </summary>
        public static void ConvertToPointerArray<TCallback>(Span<IntPtr> pointers, ICallbackable[] interfaces)
            where TCallback : ICallbackable
        {
            var arrayLength = interfaces.Length;
            Guid? callbackTypeGuid = null;

            for (var i = 0; i < arrayLength; ++i)
                pointers[i] = interfaces[i] switch
                {
                    null => IntPtr.Zero,
                    CppObject cpp => cpp.NativePointer,
                    { } item => GetShadowContainer(item).Find(callbackTypeGuid ??= GetCallbackTypeGuid())
                };

            static Guid GetCallbackTypeGuid() => ShadowContainer.GuidFromType(typeof(TCallback));
        }
    }
}