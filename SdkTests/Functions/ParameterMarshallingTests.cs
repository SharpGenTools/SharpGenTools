using System;
using Xunit;

namespace Functions
{
    public class ParameterMarshallingTests
    {
        [Fact]
        public void InterfaceOutArrays()
        {
            int num = 3;
            Interface[] results = new Interface[num];
            NativeFunctions.GetInterfaces(num, results);
            foreach (var result in results)
            {
                result.Method();
            }
        }

        [Fact]
        public void OutIntArray()
        {
            int num = 3;
            int[] results = new int[num];
            NativeFunctions.GetIntArray(num, results);

            for (int i = 0; i < num; i++)
            {
                Assert.Equal(i, results[i]);
            }
        }

        [Fact]
        public void StringMarshalling()
        {
            Assert.Equal('W', NativeFunctions.GetFirstCharacter("Wide-char test"));
        }

        [Fact]
        public void StructMarshalling()
        {
            var defaultMarshalling  = new StructWithMarshal();

            defaultMarshalling.I[1] = 6;

            var staticMarshalling = new StructWithStaticMarshal();

            staticMarshalling.I[1] = 10;

            NativeFunctions.StructMarshalling(defaultMarshalling, staticMarshalling, out var defaultOut, out var staticOut);

            Assert.Equal(defaultMarshalling.I[1], defaultOut.I[1]);
            Assert.Equal(staticMarshalling.I[1], staticOut.I[1]);
        }

        [Fact]
        public void StructArray()
        {
            var defaultMarshalling = new StructWithMarshal();
            var staticMarshalling = new StructWithStaticMarshal();
            defaultMarshalling.I[2] = 10;
            staticMarshalling.I[2] = 30;

            var defaultMarshallingOut = new StructWithMarshal();
            var staticMarshallingOut = new StructWithStaticMarshal();

            
            NativeFunctions.StructArrayMarshalling(
                new [] { defaultMarshalling },
                new [] { staticMarshalling },
                new [] { defaultMarshallingOut },
                new [] { staticMarshallingOut });

            Assert.Equal(defaultMarshalling.I[1], defaultMarshallingOut.I[1]);
            Assert.Equal(staticMarshalling.I[1], staticMarshallingOut.I[1]);
        }

        [Fact]
        public void FastOut()
        {
            var iface = new Interface(IntPtr.Zero);

            NativeFunctions.FastOutTest(iface);

            Assert.NotEqual(IntPtr.Zero, iface.NativePointer);
        }

        [Fact]
        public void Enum()
        {
            Assert.Equal(1, (int)NativeFunctions.PassThroughEnum((MyEnum)1));
        }
    }
}