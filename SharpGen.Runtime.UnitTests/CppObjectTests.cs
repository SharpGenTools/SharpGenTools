using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SharpGen.Runtime.UnitTests
{
    public class CppObjectTests
    {
        [Fact]
        public void GetCallbackPtrReturnsPointerToShadow()
        {
            using (var callback = new CallbackImpl())
            {
                Assert.NotEqual(IntPtr.Zero, MarshallingHelpers.ToCallbackPtr<ICallback>(callback));
            }
        }

        [Fact]
        public void GetCallbackPtrForClassWithMultipleInheritenceShouldReturnPointer()
        {
            using (var callback = new Callback2Impl())
            {
                Assert.NotEqual(IntPtr.Zero, MarshallingHelpers.ToCallbackPtr<ICallback>(callback));
                Assert.NotEqual(IntPtr.Zero, MarshallingHelpers.ToCallbackPtr<ICallback2>(callback));
            }
        }
    }
}
