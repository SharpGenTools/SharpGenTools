using System;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime.Win32
{
    public sealed unsafe class WinRTString : CppObject
    {
        public string Value
        {
            get
            {
                var handle = NativePointer;
                if (handle == IntPtr.Zero)
                    return null;

                var buffer = (char*) WindowsGetStringRawBuffer(handle, out var length);
                return new string(buffer, 0, (int) length);
            }
        }

        public WinRTString(IntPtr pointer) : base(pointer)
        {
        }

        public WinRTString(string value) : base(WindowsCreateString(value))
        {
        }

        protected override void DisposeCore(IntPtr nativePointer, bool disposing)
        {
            WindowsDeleteString(nativePointer);
        }

        public override string ToString() => Value;

        private const string ComBase = "combase";

        /// <summary>
        /// No documentation.
        /// </summary>
        /// <param name = "sourceString">No documentation.</param>
        /// <returns>No documentation.</returns>
        /// <unmanaged>HRESULT WindowsCreateString([In, Buffer, Optional] const wchar_t* sourceString, [In] unsigned int length, [Out, Optional] HSTRING* string)</unmanaged>
        /// <unmanaged-short>WindowsCreateString</unmanaged-short>
        private static IntPtr WindowsCreateString(string sourceString)
        {
            IntPtr text;
            Result result;
            var length = (uint)sourceString.Length;
            fixed (char* sourceString_ = sourceString)
                result = WindowsCreateString_(sourceString_, length, &text);
            result.CheckError();
            return text;
        }

        [DllImport(ComBase, EntryPoint = "WindowsCreateString", CallingConvention = CallingConvention.StdCall)]
        private static extern int WindowsCreateString_(void* sourceString, uint length, void* text);
        /// <summary>
        /// No documentation.
        /// </summary>
        /// <param name = "text">No documentation.</param>
        /// <returns>No documentation.</returns>
        /// <unmanaged>HRESULT WindowsDeleteString([In, Optional] HSTRING string)</unmanaged>
        /// <unmanaged-short>WindowsDeleteString</unmanaged-short>
        private static void WindowsDeleteString(IntPtr text)
        {
            Result result = WindowsDeleteString_((void*)text);
            result.CheckError();
        }

        [DllImport(ComBase, EntryPoint = "WindowsDeleteString", CallingConvention = CallingConvention.StdCall)]
        private static extern int WindowsDeleteString_(void* text);
        /// <summary>
        /// No documentation.
        /// </summary>
        /// <param name = "text">No documentation.</param>
        /// <param name = "length">No documentation.</param>
        /// <returns>No documentation.</returns>
        /// <unmanaged>const wchar_t* WindowsGetStringRawBuffer([In, Optional] HSTRING string, [Out, Optional] unsigned int* length)</unmanaged>
        /// <unmanaged-short>WindowsGetStringRawBuffer</unmanaged-short>
        private static IntPtr WindowsGetStringRawBuffer(IntPtr text, out uint length)
        {
            fixed (void* length_ = &length)
                return WindowsGetStringRawBuffer_((void*)text, length_);
        }

        [DllImport(ComBase, EntryPoint = "WindowsGetStringRawBuffer", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr WindowsGetStringRawBuffer_(void* text, void* length);
    }
}