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

namespace SharpGen.Runtime;

/// <summary>
/// Base class for all callback objects written in managed code.
/// </summary>
public abstract class CallbackBase : DisposeBase, ICallbackable
{
#if NET5_0_OR_GREATER
    private uint _refCount = 1;
#else
    private int _refCount = 1;
#endif
    private bool _isDisposed;

    protected override bool IsDisposed => _isDisposed;

    /// <inheritdoc />
    protected sealed override void Dispose(bool disposing)
    {
        DisposeCore(disposing);

        if (disposing)
            Release();

        // Good idea would be to get rid of the _isDisposed field and use refCount <= 0 condition instead.
        // That's a dangerous change not to be made lightly.
        _isDisposed = true;
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void DisposeCore(bool disposing)
    {
    }

    public uint AddRef()
    {
#if NET5_0_OR_GREATER
        return Interlocked.Increment(ref _refCount);
#else
        return (uint)Interlocked.Increment(ref _refCount);
#endif
    }

    public uint Release()
    {
#if NET5_0_OR_GREATER
        var newRefCount = Interlocked.Decrement(ref _refCount);
#else
        var newRefCount = Interlocked.Decrement(ref _refCount);
#endif
        if (newRefCount == 0)
        {
            // Dispose native resources
            Interlocked.Exchange(ref shadow, null)?.Dispose();
        }
        return (uint) newRefCount;
    }

    public override string ToString() =>
        $"{GetType().Name}[{RuntimeHelpers.GetHashCode(this):X}:{Volatile.Read(ref _refCount)}]";

    private ShadowContainer shadow;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ShadowContainer Shadow
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