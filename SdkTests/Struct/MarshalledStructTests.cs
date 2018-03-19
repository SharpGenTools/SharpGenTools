using System;
using Xunit;

namespace Struct
{
    public class MarshalledStructTests
    {
        [Fact]
        public void StructWithArrayMakesArrayCorrectLength()
        {
            var obj = new StructWithArray();
            
            Assert.Equal(3, obj.I.Length);
        }

        [Fact]
        public void StructWithArrayMarshalsCorrectly()
        {
            var obj = new StructWithArray
            {
                J = 8.0
            };

            obj.I[0] = 3;
            obj.I[1] = 5;
            obj.I[2] = 42;

            var result = Functions.PassThrough(obj);

            Assert.Equal(obj.I[0], result.I[0]);
            Assert.Equal(obj.I[1], result.I[1]);
            Assert.Equal(obj.I[2], result.I[2]);
            Assert.Equal(obj.J, result.J);
        }

        [Fact]
        public void UnionMarshalsCorrectly()
        {
            var obj = new TestUnion
            {
                Decimal = 2.0f
            };

            Assert.Equal(obj.Integer, Functions.PassThrough(obj).Integer);
        }

        [Fact]
        public void BitFieldMarshalsCorrectly()
        {
            var obj = new BitField
            {
                FirstBit = true,
                LastBits = 42
            };

            var result = Functions.PassThrough(obj);
            Assert.Equal(obj.FirstBit, result.FirstBit);
            Assert.Equal(obj.LastBits, obj.LastBits);
        }

        [Fact]
        public void InplaceAsciiStringMarshalsCorrectly()
        {
            var obj = new AsciiTest
            {
                SmallString = "Test"
            };

            Assert.Equal(obj.SmallString, Functions.PassThrough(obj).SmallString);
        }

        [Fact]
        public void InplaceAsciiStringTruncatedToFit()
        {
            var obj = new AsciiTest
            {
                SmallString = "123456789011"
            };

            var result = Functions.PassThrough(obj);

            Assert.Equal("1234567890", result.SmallString);
            Assert.Null(result.LargeString);
        }

        [Fact]
        public void InplaceUtf16StringMarshalsCorrectly()
        {
            var obj = new Utf16Test
            {
                SmallString = "Test"
            };

            Assert.Equal(obj.SmallString, Functions.PassThrough(obj).SmallString);
        }

        [Fact]
        public void AsciiStringMarshalsCorrectly()
        {
            var obj = new AsciiTest
            {
                LargeString = "This is a test"
            };

            Assert.Equal(obj.LargeString, Functions.PassThrough(obj).LargeString);
        }

        [Fact]
        public void UtfStringMarshalsCorrectly()
        {
            var obj = new Utf16Test
            {
                LargeString = "This is a test"
            };

            Assert.Equal(obj.LargeString, Functions.PassThrough(obj).LargeString);
        }
    }
}
