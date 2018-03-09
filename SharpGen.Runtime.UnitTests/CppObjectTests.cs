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
                Assert.NotEqual(IntPtr.Zero, CppObject.ToCallbackPtr<ICallback>(callback));
            }
        }
    }
}
