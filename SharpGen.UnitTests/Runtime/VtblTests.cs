using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Xunit;

namespace SharpGen.UnitTests.Runtime
{
    public class VtblTests
    {
        [Fact]
        public void CanRoundTripCallThroughNativeVtblToManagedObject()
        {
            using var callback = new CallbackImpl();

            var callbackPtr = MarshallingHelpers.ToCallbackPtr<ICallback>(callback);
            Assert.NotEqual(IntPtr.Zero, callbackPtr);

            var methodPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(callbackPtr));
            var delegateObject = Marshal.GetDelegateForFunctionPointer<CallbackVtbl.IncrementDelegate>(methodPtr);
            Assert.Equal(3, delegateObject(callbackPtr, 2));
        }
    }
}
