using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGen.Runtime.UnitTests
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
                AddMethod(new IncrementDelegate(IncrementImpl));
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
}
