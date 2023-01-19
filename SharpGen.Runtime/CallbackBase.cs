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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SharpGen.Runtime;

/// <summary>
/// Base class for all callback objects written in managed code.
/// It is the container used to keep track of all native (e.g. COM) callbacks for a single managed object.
/// All managed callbacks must inherit from this class.
/// </summary>
public abstract unsafe partial class CallbackBase : DisposeBase, ICallbackable
{
    private Dictionary<Guid, IntPtr>? _ccw;
    private IntPtr _guidPtr;
    private IntPtr[]? _guids;
#if NET6_0_OR_GREATER
    private uint _refCount = 1;
#else
    private int _refCount = 1;
#endif
    private bool _isDisposed;
    private GCHandle _thisHandle;

    protected sealed override bool IsDisposed => _isDisposed;

    /// <inheritdoc />
    protected sealed override void Dispose(bool disposing)
    {
        DisposeCore(disposing);

        if (disposing)
        {
            Release();
        }
        else
        {
            // Dispose native resources
            DisposeCallableWrappers(false);
        }

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
#if NET6_0_OR_GREATER
        return Interlocked.Increment(ref _refCount);
#else
        return (uint)Interlocked.Increment(ref _refCount);
#endif
    }

    public uint Release()
    {
#if NET6_0_OR_GREATER
        var newRefCount = Interlocked.Decrement(ref _refCount);
#else
        var newRefCount = Interlocked.Decrement(ref _refCount);
#endif
        if (newRefCount == 0)
        {
            // Dispose native resources
            DisposeCallableWrappers(true);
        }
        return (uint) newRefCount;
    }

    public override string ToString() =>
        $"{GetType().Name}[{RuntimeHelpers.GetHashCode(this):X}:{Volatile.Read(ref _refCount)}]";

    public ReadOnlySpan<IntPtr> Guids
    {
        get
        {
            if (_guids is { } guids)
                return guids;

            var guidList = BuildGuidList();
            var guidCount = guidList.Length;
            var guidPtr = Marshal.AllocHGlobal(sizeof(Guid) * guidCount);
            var pGuid = (Guid*) guidPtr;
            guids = new IntPtr[guidCount];
            for (var i = 0; i < guidCount; i++)
            {
                pGuid[i] = guidList[i];
                guids[i] = new IntPtr(pGuid + i);
            }

            // This property is not thread-safe, but can easily be fixed if need arises.
            _guidPtr = guidPtr;
            _guids = guids;

            return guids;
        }
    }

    public IntPtr Find(Guid guidType)
    {
        if (_ccw is not { } guidToShadow)
        {
            guidToShadow = _ccw = new(4);
            Debug.Assert(!_thisHandle.IsAllocated);
            InitializeCallableWrappers(_ccw);
        }

        return guidToShadow.TryGetValue(guidType, out var shadow) ? shadow : IntPtr.Zero;
    }

    public IntPtr Find<TCallback>() where TCallback : ICallbackable => Find(TypeDataStorage.GetGuid<TCallback>());

    protected GCHandle ThisHandle => _thisHandle switch
    {
        { IsAllocated: true } handle => handle,
        _ => _thisHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection)
    };

    protected static IntPtr CreateCallableWrapper(void* vtbl, GCHandle callback)
    {
        Debug.Assert(vtbl != (void*) 0);
        Debug.Assert(callback.IsAllocated);
        return CppObjectCallableWrapper.Create(vtbl, callback);
    }

    protected static GCHandle CreateMultiInheritanceShadow(params GCHandle[] shadows)
    {
        if (shadows == null) throw new ArgumentNullException(nameof(shadows));
        return shadows.Length switch
        {
            0 => throw new ArgumentException(
                     "Multi-inheritance shadow cannot be constructed for empty shadow collection.",
                     nameof(shadows)
                 ),
            1 => throw new ArgumentException(
                     "Multi-inheritance shadow cannot be constructed for a single shadow.",
                     nameof(shadows)
                 ),
            _ => GCHandle.Alloc(new CppObjectMultiShadow(shadows), GCHandleType.Normal)
        };
    }

    private void DisposeCallableWrappers(bool disposing)
    {
        if (Interlocked.Exchange(ref _ccw, null) is { Values: { } shadows })
        {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1 || NET472
            HashSet<IntPtr> freed = new(shadows.Count);
#else
            HashSet<IntPtr> freed = new();
#endif
            foreach (var comObjectCallbackNative in shadows)
                if (freed.Add(comObjectCallbackNative))
                    CppObjectCallableWrapper.Free(comObjectCallbackNative, disposing);
        }

        if (Interlocked.Exchange(ref _guidPtr, default) is var pointer)
            Marshal.FreeHGlobal(pointer);

        if (_thisHandle.IsAllocated)
            _thisHandle.Free();
    }

    internal IReadOnlyCollection<CppObjectShadow> Shadows
    {
        get
        {
            Debug.Assert(_ccw is not null);

            HashSet<CppObjectShadow> shadows = new(ReferenceEqualityComparer.Instance);

            foreach (CppObjectCallableWrapper* ccw in _ccw!.Values)
            {
                var handle = ccw->Shadow;
                if (!handle.IsAllocated)
                    continue;

                switch (handle.Target)
                {
                    case CppObjectShadow shadow:
                        shadows.Add(shadow);
                        break;
                    case CppObjectMultiShadow multiShadow:
                        multiShadow.AddShadowsToSet(shadows);
                        break;
                }
            }

#if NET45
            return shadows.ToArray();
#else
            return shadows;
#endif
        }
    }
}