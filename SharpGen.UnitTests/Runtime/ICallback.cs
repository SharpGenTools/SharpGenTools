using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace SharpGen.UnitTests.Runtime
{
    [Shadow(typeof(CallbackShadow))]
    interface ICallback : ICallbackable
    {
        int Increment(int param);
    }

    class CallbackImpl : CallbackBase, ICallback
    {
        public int Increment(int param)
        {
            return param + 1;
        }
    }

    class CallbackShadow : CppObjectShadow
    {
        public class CallbackVbtl : CppObjectVtbl
        {
            public CallbackVbtl(int numberOfCallbackMethods) : base(numberOfCallbackMethods + 1)
            {
                AddMethod(new IncrementDelegate(IncrementImpl), 0);
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IncrementDelegate(IntPtr thisObj, int param);

            private static int IncrementImpl(IntPtr thisObj, int param)
            {
                var shadow = ToShadow<CallbackShadow>(thisObj);
                var callback = (ICallback)shadow.Callback;
                return callback.Increment(param);
            }
        }

        protected override CppObjectVtbl Vtbl { get; } = new CallbackVbtl(0);
    }

    [Shadow(typeof(Callback2Shadow))]
    interface ICallback2: ICallback
    {
        int Decrement(int param);
    }

    class Callback2Shadow : CppObjectShadow
    {
        public class Callback2Vbtl : CallbackShadow.CallbackVbtl
        {
            public Callback2Vbtl(int numberOfCallbackMethods) : base(numberOfCallbackMethods + 1)
            {
                AddMethod(new DecrementDelegate(DecrementImpl), 1);
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int DecrementDelegate(IntPtr thisObj, int param);

            private static int DecrementImpl(IntPtr thisObj, int param)
            {
                var shadow = ToShadow<CallbackShadow>(thisObj);
                var callback = (ICallback)shadow.Callback;
                return callback.Increment(param);
            }
        }

        protected override CppObjectVtbl Vtbl { get; } = new Callback2Vbtl(0);
    }

    class Callback2Impl : CallbackImpl, ICallback, ICallback2
    {
        public int Decrement(int param)
        {
            return param - 1;
        }
    }
}
