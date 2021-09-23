using System.Collections;
using System.Collections.Generic;

namespace SharpGen.Runtime.Win32
{
    partial class IEnumUnknown : IEnumerable<IUnknown>
    {
        public uint Next(IUnknown[] rgelt)
        {
            Next(rgelt, out var fetched).CheckError();

            return fetched;
        }

        public IEnumerator<IUnknown> GetEnumerator() => new ComObjectEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}