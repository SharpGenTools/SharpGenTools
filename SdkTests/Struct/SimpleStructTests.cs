using System;
using Xunit;

namespace Struct
{
    public class SimpleStructTests
    {
        [Fact]
        public void SimpleStructMarshalledCorrectly()
        {
            var simple = Functions.GetSimpleStruct();
            Assert.Equal(10, simple.I);
            Assert.Equal(3, simple.J);
        }
    }
}
