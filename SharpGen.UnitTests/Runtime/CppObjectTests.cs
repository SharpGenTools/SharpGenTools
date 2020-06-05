using System;
using SharpGen.Runtime;
using Xunit;

namespace SharpGen.UnitTests.Runtime
{
    public class CppObjectTests
    {
        [Fact]
        public void GetCallbackPtrReturnsPointerToShadow()
        {
            using var callback = new CallbackImpl();

            Assert.NotEqual(IntPtr.Zero, CppObject.ToCallbackPtr<ICallback>(callback));
        }

        [Fact]
        public void GetCallbackPtrForClassWithMultipleInheritenceShouldReturnPointer()
        {
            using var callback = new Callback2Impl();

            Assert.NotEqual(IntPtr.Zero, CppObject.ToCallbackPtr<ICallback>(callback));
            Assert.NotEqual(IntPtr.Zero, CppObject.ToCallbackPtr<ICallback2>(callback));
        }
    }
}
