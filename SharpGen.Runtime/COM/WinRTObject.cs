using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime.Win32;

namespace SharpGen.Runtime
{
    public partial class WinRTObject
    {
        public Guid[] Iids
        {
            get
            {
                GetIids(out var count, out var iids);
                var iid = new Guid[count];
                MemoryHelpers.Read<Guid>(iids, iid, (int) count);
                Marshal.FreeCoTaskMem(iids);
                return iid;
            }
        }

        public string RuntimeClassName
        {
            get
            {
                var nativeStringPtr = GetRuntimeClassName();
                using WinRTString nativeString = new(nativeStringPtr);
                return nativeString.Value;
            }
        }
    }
}