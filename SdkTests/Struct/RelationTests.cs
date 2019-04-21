using System;
using Xunit;

namespace Struct
{
    public class RelationTests
    {
        [Fact]
        public unsafe void StructSize()
        {
            var str = default(StructSizeRelation);

            var native = default(StructSizeRelation.__Native);

            str.__MarshalTo(ref native);

            Assert.Equal(sizeof(StructSizeRelation.__Native), native.CbSize);
        }

        [Fact]
        public void Constant()
        {
            var str = default(ReservedRelation);

            var native = default(ReservedRelation.__Native);

            str.__MarshalTo(ref native);

            Assert.Equal(42, native.Reserved);
        }
    }
}
