using System;
using SharpGen.Runtime;
using Xunit;

namespace Functions
{
    public class RelationTests
    {
        [Fact]
        public void ValueType()
        {
            var test = new []
            {
                new SimpleStruct {I = 1},
                new SimpleStruct {I = 2},
                new SimpleStruct {I = 3}
            };
            
            Assert.Equal(6, NativeFunctions.Sum(test));
        }
    }
}