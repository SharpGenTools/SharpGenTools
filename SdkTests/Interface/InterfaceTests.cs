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

        [Fact]
        public void GuidCorrectlyAssociatedWithInterface()
        {
            Assert.Equal(Guid.Parse("{16410F4E-B4AB-4B33-B9A3-7FC8FA15F4F4}"), typeof(IInterfaceWithGuid).GUID);
        }

        [Fact]
        public void InnerInterfaceNativePointerKeptUpToDate()
        {
            var largeInterface = new ILargeInterface(IntPtr.Zero);

            Assert.Equal(largeInterface.NativePointer, largeInterface.Inner.NativePointer);

            largeInterface.NativePointer = new IntPtr(1);

            Assert.Equal(largeInterface.NativePointer, largeInterface.Inner.NativePointer);
        }

        [Fact]
        public void FastOutInterfaceTest()
        {
            var impl = new FastOutInterfaceNative(IntPtr.Zero);

            Functions.FastOutInterfaceTest(impl);

            Assert.NotEqual(IntPtr.Zero, impl.NativePointer);
        }
    }
}
