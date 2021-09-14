using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime.Win32
{
    partial class IEnumString : IEnumerable<string>
    {
        public unsafe uint Next(string[] rgelt)
        {
            var length = rgelt.Length;
            var celt = (uint) length;
            Span<IntPtr> rgelt_ = stackalloc IntPtr[0];
            {
                rgelt_ = (uint) length * (uint) IntPtr.Size < 1024U
                             ? stackalloc IntPtr[length]
                             : new IntPtr[length];
            }

            uint fetched;

            fixed (void* _rgelt = rgelt_)
                Next(celt, new IntPtr(_rgelt), out fetched).CheckError();

            for (var i = 0; i < fetched; ++i)
                rgelt[i] = Marshal.PtrToStringUni(rgelt_[i]);

            return fetched;
        }

        public IEnumerator<string> GetEnumerator() => new ComStringEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}