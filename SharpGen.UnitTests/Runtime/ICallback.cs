using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace SharpGen.UnitTests.Runtime;

[Vtbl(typeof(CallbackVtbl))]
interface ICallback : ICallbackable
{
    int Increment(int param);
}

class CallbackImpl : CallbackBase, ICallback
{
    public int Increment(int param) => param + 1;
}

public static class CallbackVtbl
{
    private static readonly IncrementDelegate Increment = IncrementImpl;

    public static readonly IntPtr[] Vtbl =
    {
        Marshal.GetFunctionPointerForDelegate(Increment)
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int IncrementDelegate(IntPtr thisObj, int param);

    private static int IncrementImpl(IntPtr thisObj, int param) =>
        CppObjectShadow.ToCallback<ICallback>(thisObj).Increment(param);
}

[Vtbl(typeof(Callback2Vtbl))]
interface ICallback2: ICallback
{
    int Decrement(int param);
}

public static class Callback2Vtbl
{
    private static readonly DecrementDelegate Decrement = DecrementImpl;

    public static IntPtr[] Vtbl { get; } =
    {
        Marshal.GetFunctionPointerForDelegate(Decrement)
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int DecrementDelegate(IntPtr thisObj, int param);

    private static int DecrementImpl(IntPtr thisObj, int param) =>
        CppObjectShadow.ToCallback<ICallback>(thisObj).Increment(param);
}

class Callback2Impl : CallbackImpl, ICallback, ICallback2
{
    public int Decrement(int param) => param - 1;
}