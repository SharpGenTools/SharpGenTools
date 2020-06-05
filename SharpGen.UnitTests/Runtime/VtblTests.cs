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

            var callbackPtr = CppObject.ToCallbackPtr<ICallback>(callback);
            var methodPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(callbackPtr));
            var delegateObject = Marshal.GetDelegateForFunctionPointer<CallbackShadow.CallbackVbtl.IncrementDelegate>(methodPtr);
            Assert.Equal(3, delegateObject(callbackPtr, 2));
        }
    }
}
