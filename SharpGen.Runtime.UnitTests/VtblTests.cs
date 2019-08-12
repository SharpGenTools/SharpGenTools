using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SharpGen.Runtime.UnitTests
{
    public class VtblTests
    {
        [Fact]
        public void CanRoundTripCallThroughNativeVtblToManagedObject()
        {
            using (var callback = new CallbackImpl())
            {
                var callbackPtr = MarshallingHelpers.ToCallbackPtr<ICallback>(callback);
                var methodPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(callbackPtr));
                var delegateObject = Marshal.GetDelegateForFunctionPointer<CallbackShadow.CallbackVbtl.IncrementDelegate>(methodPtr);
                Assert.Equal(3, delegateObject(callbackPtr, 2));
            }
        }
    }
}
