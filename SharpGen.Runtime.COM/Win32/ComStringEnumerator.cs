using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpGen.Runtime.Win32
{
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public struct ComStringEnumerator : IEnumerator<string>
    {
        // .NET Native has issues with <...> in property backing fields in structs
        private readonly IEnumString _impl;
        private string _current;

        public ComStringEnumerator(IEnumString impl)
        {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _current = null;
        }

        public unsafe bool MoveNext()
        {
            IntPtr output;
            var hasNext = _impl.Next(1, new IntPtr(&output), out var fetched).Success;
            Current = hasNext && fetched == 1 ? Marshal.PtrToStringUni(output) : null;
            return hasNext;
        }

        public void Reset() => _impl.Reset();

        public string Current
        {
            get => _current;
            private set => _current = value;
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}