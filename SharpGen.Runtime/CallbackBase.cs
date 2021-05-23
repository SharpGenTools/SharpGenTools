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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SharpGen.Runtime
{
    /// <summary>
    /// Base class for all callback objects written in managed code.
    /// </summary>
    public abstract class CallbackBase : DisposeBase, ICallbackable
    {
        private int refCount = 1;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Release();
            }
        }

        public uint AddRef()
        {
#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine(
                    $"{GetType().Name}[{RuntimeHelpers.GetHashCode(this):X}:{Volatile.Read(ref refCount)}]::{nameof(AddRef)}"
                );
#endif
            return (uint) Interlocked.Increment(ref refCount);
        }

        public uint Release()
        {
#if DEBUG
            if (Configuration.EnableObjectLifetimeTracing)
                Debug.WriteLine(
                    $"{GetType().Name}[{RuntimeHelpers.GetHashCode(this):X}:{Volatile.Read(ref refCount)}]::{nameof(Release)}"
                );
#endif
            var newRefCount = Interlocked.Decrement(ref refCount);
            if (newRefCount == 0)
            {
                // Dispose native resources
                Interlocked.Exchange(ref shadow, null)?.Dispose();
            }
            return (uint) newRefCount;
        }

        private ShadowContainer shadow;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ShadowContainer ICallbackable.Shadow
        {
            get
            {
                var oldValue = Volatile.Read(ref shadow);
                if (oldValue != null)
                    return oldValue;

                ShadowContainer value = new(this);

                // Only set the shadow container if it is not already set.
                oldValue = Interlocked.CompareExchange(ref shadow, value, null);
                if (oldValue == null)
                    return value;

                // shadow remains set to an already initialized oldValue
                value.Dispose();
                return oldValue;
            }
        }
    }
}