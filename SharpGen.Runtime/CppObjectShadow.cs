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

namespace SharpGen.Runtime;

/// <summary>
/// A callback interface shadow, used to supplement interface implementers with additional logic,
/// possibly to modify marshalling behavior.
/// Using shadows incurs minor negative memory and performance impact, so use sparingly.
/// In terms of .NET runtime, it's a COM Callable Wrapper (CCW).
/// </summary>
public abstract unsafe class CppObjectShadow
{
    private GCHandle callbackHandle;

    static CppObjectShadow()
    {
        Debug.Assert(sizeof(CppObjectCallableWrapper) == CppObjectCallableWrapper.Size);
    }

    protected CppObjectShadow()
    {
        Debug.Assert(this is not ICallbackable);
    }

    internal void Initialize(GCHandle callbackHandle)
    {
        Debug.Assert(!this.callbackHandle.IsAllocated);
        Debug.Assert(callbackHandle.IsAllocated);
        Debug.Assert(callbackHandle.Target is CallbackBase);
        Debug.Assert(callbackHandle.Target is ICallbackable);
        this.callbackHandle = callbackHandle;
    }

    public static T ToAnyShadow<T>(IntPtr thisPtr) where T : CppObjectShadow
    {
        Debug.Assert(thisPtr != IntPtr.Zero);

        var handle = ((CppObjectCallableWrapper*) thisPtr)->Shadow;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T shadow => shadow,
            CppObjectMultiShadow multiShadow => multiShadow.ToShadow<T>() ?? throw new Exception($"Shadow {typeof(T).FullName} not found in the inheritance graph"),
            CallbackBase callback => callback.Shadows.OfType<T>().FirstOrDefault() ?? throw new Exception($"Shadow {typeof(T).FullName} not found in the inheritance graph"),
            null => throw new Exception($"Shadow {typeof(T).FullName} is dead"),
            ICallbackable value => throw new Exception(
                             $"Shadow is of an unexpected {nameof(ICallbackable)} type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         ),
            { } value => throw new Exception(
                             $"Shadow is of an unexpected type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }

    public static IEnumerable<T> ToAllShadows<T>(IntPtr thisPtr) where T : CppObjectShadow =>
        ToCallback<CallbackBase>(thisPtr).Shadows.OfType<T>();

    public IEnumerable<T> ToAllShadows<T>() where T : CppObjectShadow => ToCallback<CallbackBase>().Shadows.OfType<T>();

    public static T ToCallback<T>(IntPtr thisPtr) where T : ICallbackable
    {
        Debug.Assert(thisPtr != IntPtr.Zero);

        var handle = ((CppObjectCallableWrapper*) thisPtr)->Shadow;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T value => value,
            CppObjectShadow shadow => shadow.ToCallback<T>(),
            CppObjectMultiShadow multiShadow when multiShadow.ToCallback(out T? callback) => callback,
            CppObjectMultiShadow => throw new Exception($"Shadow {typeof(T).FullName} is missing the callback in the whole inheritance graph"),
            null => throw new Exception($"Shadow {typeof(T).FullName} is dead"),
            { } value => throw new Exception(
                             $"Shadow is of an unexpected type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }

    public T ToCallback<T>() where T : ICallbackable
    {
        var handle = callbackHandle;
        Debug.Assert(handle.IsAllocated);
        return handle.Target switch
        {
            T value => value,
            null => throw new Exception($"Shadow {typeof(T).FullName} references dead callback"),
            { } value => throw new Exception(
                             $"Shadow references an unexpected callback of type {value.GetType().FullName}, expected {typeof(T).FullName}"
                         )
        };
    }
}