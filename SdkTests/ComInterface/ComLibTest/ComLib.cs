using System;
using Xunit;

namespace ComLibTest
{
    public class ComLib
    {
        [Fact]
        public void Test1()
        {
            using (var inst = Functions.CreateInstance())
            {
                var value = inst.GetValue();
                Assert.Equal(1, value.I);
                Assert.Equal(3.0, value.J);
            }
        }
    }
}
