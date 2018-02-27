using System;
using Xunit;

namespace Interface
{
    public class InterfaceTests
    {
        [Fact]
        public void BasicMethodCall()
        {
            using (var inst = Functions.CreateInstance())
            {
                var value = inst.GetValue2();
                Assert.Equal(1, value.I);
                Assert.Equal(3.0, value.J);
            }
        }
    }
}
