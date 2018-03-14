using System;
using SharpGen.Runtime;
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
                var value = inst.Value2;
                Assert.Equal(1, value.I);
                Assert.Equal(3.0, value.J);
            }
        }

        [Fact]
        public void MethodWithParameters()
        {
            using (var inst = Functions.CreateInstance2(3, 4))
            {
                var value = inst.Value;
                Assert.Equal(3, value.I);
                Assert.Equal(4.0, value.J);
            }
        }

        [Fact]
        public void AutoOutParameters()
        {
            using (var inst = Functions.CreateInstance2(1, 5))
            {
                var value = inst.Value;
                Assert.True(Functions.CloneInstance(inst, out var cloned));
                using(cloned)
                {
                    var clonedValue = cloned.Value;
                    Assert.Equal(value.I, clonedValue.I);
                    Assert.Equal(value.J, clonedValue.J);
                }
            }
        }
        
        [Fact]
        public void InterfaceArray()
        {
            using (var inst = Functions.CreateInstance())
            {
                inst.AddToThis(new InterfaceArray<NativeInterface2>(), 0);
            }
        }
    }
}
