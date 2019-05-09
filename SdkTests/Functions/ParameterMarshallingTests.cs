using System;
using SharpGen.Runtime;
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
        public void InterfaceOutArraysOptional()
        {
            int num = 3;
            NativeFunctions.GetInterfacesOptional(num, null);
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
            Assert.Equal((byte)'A', NativeFunctions.GetFirstAnsiCharacter("Ansi-char test"));
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
            var defaultMarshalling = new[] { new StructWithMarshal() };
            var staticMarshalling = new[] { new StructWithStaticMarshal() };
            defaultMarshalling[0].I[1] = 10;
            staticMarshalling[0].I[1] = 30;

            var defaultMarshallingOut = new[] { new StructWithMarshal() };
            var staticMarshallingOut = new[] { new StructWithStaticMarshal() };

            
            NativeFunctions.StructArrayMarshalling(
                defaultMarshalling,
                staticMarshalling ,
                defaultMarshallingOut,
                staticMarshallingOut );

            Assert.Equal(defaultMarshalling[0].I[1], defaultMarshallingOut[0].I[1]);
            Assert.Equal(staticMarshalling[0].I[1], staticMarshallingOut[0].I[1]);
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
            Assert.Equal(1, (int)NativeFunctions.PassThroughEnum(MyEnum.TestValue));
        }

        [Fact]
        public void EnumOut()
        {
            NativeFunctions.EnumOut(out MyEnum value);

            Assert.Equal(MyEnum.TestValue, value);
        }

        [Fact]
        public void RefParameterTest()
        {
            int i = 1;
            NativeFunctions.Increment(ref i);
            Assert.Equal(2, i);
        }

        [Fact]
        public void NullableParameterTest()
        {
            Assert.Equal(2, NativeFunctions.Add(2, null));
            Assert.Equal(2, NativeFunctions.Add(1, 1));
        }

        [Fact]
        public void BoolToIntTest()
        {
            Assert.True(NativeFunctions.BoolToIntTest(true));
            Assert.False(NativeFunctions.BoolToIntTest(false));
        }

        [Fact]
        public void BoolArrayTest()
        {
            var inArray = new bool[2];
            inArray[1] = true;

            var outArray = new bool[2];

            NativeFunctions.BoolArrayTest(inArray, outArray, 2);
            Assert.False(outArray[0]);
            Assert.True(outArray[1]);
        }

        [Fact]
        public void StringReturnTest()
        {
            Assert.Equal("Functions", NativeFunctions.GetName());
        }

        [Fact]
        public void StructRefParameter()
        {
            var csStruct = new StructWithMarshal();

            NativeFunctions.SetAllElements(ref csStruct);

            Assert.Equal(10, csStruct.I[0]);
            Assert.Equal(10, csStruct.I[1]);
            Assert.Equal(10, csStruct.I[2]);
        }

        [Fact]
        public void NullableStructParameter()
        {
            Assert.Equal(0, NativeFunctions.FirstElementOrZero(null));
            
            var csStruct = new StructWithMarshal();
            csStruct.I[0] = 4;

            Assert.Equal(csStruct.I[0], NativeFunctions.FirstElementOrZero(csStruct));
        }

        [Fact]
        public void OptionalArrayOfStruct()
        {
            var elements = new [] { new SimpleStruct{ I = 1 } };
            Assert.Equal(1, NativeFunctions.Sum(1, elements));
            Assert.Equal(0, NativeFunctions.Sum(0, null));
        }

        [Fact]
        public void ParamsArray()
        {
            Assert.Equal(10, NativeFunctions.Product(1, new SimpleStruct{ I = 10 }));
        }

        [Fact]
        public void ForcePassByValueParameter()
        {
            var value = new LargeStruct();

            value.I[0] = 10;
            value.I[1] = 20;
            value.I[2] = 30;

            Assert.Equal(60, NativeFunctions.SumValues(value));
        }

        [Fact]
        public void PointerSize()
        {
            var value = new PointerSize(20);

            Assert.Equal(new PointerSize(20), NativeFunctions.PassThroughPointerSize(value));
        }

        [Fact]
        public void OptionalStructArrayOut()
        {
            NativeFunctions.StructArrayOut(new StructWithMarshal(), null);
        }

        [Fact]
        public void ArrayOfStructAsClass()
        {
            var test = new [] { new StructAsClass {I = 1}, new StructAsClass {I = 2}};
            Assert.Equal(3, NativeFunctions.SumInner(test, 2));
        }

        [Fact]
        public void WrappedStructAsClass()
        {
            Assert.Equal(1, NativeFunctions.GetWrapper().Wrapped.I);
        }

        [Fact]
        public unsafe void OptionalInOutStructPointer()
        {
            var test = new SimpleStruct { I = 5 };

            void* addr = &test;

            NativeFunctions.AddOne((IntPtr)addr);

            Assert.Equal(6, test.I);
        }

        [Fact]
        public void EnumArrayTest()
        {
            var test = new [] { MyEnum.TestValue, MyEnum.TestValue };

            Assert.Equal(MyEnum.TestValue, NativeFunctions.FirstEnumElement(test));
        }
    }
}