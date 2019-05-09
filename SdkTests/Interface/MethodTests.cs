using System;
using SharpGen.Runtime;
using Xunit;

namespace Interface
{
    public class MethodTests
    {
        [Fact]
        public void PointerSizeMethodReturnTest()
        {
            using (var target = Functions.GetPointerSizeTest())
            {
                Assert.Equal(new PointerSize(25), target.PassThrough(new PointerSize(25)));
            }
        }
    }
}
