using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SharpGen.Runtime.Win32
{
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public struct ComObjectEnumerator : IEnumerator<IUnknown>
    {
        // .NET Native has issues with <...> in property backing fields in structs
        private readonly IEnumUnknown _impl;
        private IUnknown _current;

        public ComObjectEnumerator(IEnumUnknown impl)
        {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _current = null;
        }

        public bool MoveNext()
        {
            var output = new IUnknown[1];
            var hasNext = _impl.Next(output, out var fetched).Success;
            Current = hasNext && fetched == 1 ? output[0] : null;
            return hasNext;
        }

        public void Reset() => _impl.Reset();

        public IUnknown Current
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