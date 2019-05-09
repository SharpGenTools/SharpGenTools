using System;
using SharpGen.Runtime;
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
        public void UnionWithArrayMemberMarshalsCorrectly()
        {
            var obj = new UnionWithArray();
            
            obj.Parts[0] = 20u;

            obj.Parts[1] = 40u;

            var result = Functions.PassThrough(obj);

            Assert.Equal(obj.Parts[0], result.Parts[0]);
            Assert.Equal(obj.Parts[1], result.Parts[1]);
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

            Assert.Equal("123456789", result.SmallString);
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

        [Fact]
        public void StructWithNestedMarshalTypeMarshalsCorrectly()
        {
            var obj = new NestedTest
            {
                Ascii = new AsciiTest
                {
                    SmallString = "Test"
                },
                Utf = new Utf16Test
                {
                    SmallString = "Test"
                }
            };

            var result = Functions.PassThrough(obj);

            Assert.Equal(obj.Ascii.SmallString, result.Ascii.SmallString);
            Assert.Equal(obj.Utf.SmallString, result.Utf.SmallString);
        }

        [Fact]
        public void BitFieldWithMarshalTypeMarshalsCorrectly()
        {
            var obj = new BitField2
            {
                ReservedBits = 20
            };
            
            Assert.True(Functions.VerifyReservedBits(obj));

            obj.UpperBits = 4;
            obj.LowerBits = 10;

            Assert.True(Functions.VerifyReservedBits(obj));
        }

        [Fact]
        public void BoolToIntMarshalsCorrectly()
        {
            var obj = new BoolToInt2
            {
                Test = true
            };

            Assert.True(Functions.PassThrough(obj).Test);
        }

        [Fact]
        public void BoolToIntShortcutCorrectlyTracksValue()
        {
            var obj = new BoolToInt
            {
                Test = true
            };

            Assert.Equal(1, obj._Test);
        }

        [Fact]
        public void BoolArrayMemberMarshalsCorrectly()
        {
            var obj = new BoolArray();

            obj.Elements[0] = true;

            obj.Elements[1] = true;

            var result = Functions.PassThrough(obj);

            Assert.Equal(obj.Elements[0], result.Elements[0]);
            Assert.Equal(obj.Elements[1], result.Elements[1]);
            Assert.Equal(obj.Elements[2], result.Elements[2]);
        }

        [Fact]
        public void InterfaceField()
        {
            var obj = Functions.GetStructWithInterface();

            Assert.Equal(1, obj.Test.One());

            var result = Functions.PassThrough(obj);

            Assert.Equal(1, obj.Test.One());
        }

        [Fact]
        public void PointerSizeMember()
        {
            var obj = new PointerSizeMember
            {
                PointerSize = new PointerSize(25)
            };

            Assert.Equal(new PointerSize(25), Functions.PassThrough(obj).PointerSize);
        }
    }
}
