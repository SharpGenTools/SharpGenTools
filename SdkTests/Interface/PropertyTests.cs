using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Interface
{
    public class PropertyTests
    {
        [Fact]
        public void IsMethodGeneratesIsProperty()
        {
            using (var value = Functions.CreatePropertyTest(true, 0, 0))
            {
                Assert.True(value.IsTrue);
            }
        }

        [Fact]
        public void IsMethodWithOutParamGeneratesIsProperty()
        {
            using (var value = Functions.CreatePropertyTest(true, 0, 0))
            {
                Assert.True(value.IsTrueOutProp);
            }
        }

        [Fact]
        public void GetSetMethodsGenerateProperty()
        {
            using (var value = Functions.CreatePropertyTest(false, 10, 0))
            {
                Assert.Equal(10, value.Value);

                value.Value = 15;

                Assert.Equal(15, value.Value);
            }
        }

        [Fact]
        public void GetSetMethodsWithOutParamGenerateProperty()
        {
            using (var value = Functions.CreatePropertyTest(false, 0, 10))
            {
                Assert.Equal(10, value.Value2);

                value.Value2 = 15;

                Assert.Equal(15, value.Value2);
            }
        }

        [Fact]
        public void PersistentPropertyCachesFirstValue()
        {
            using (var value = Functions.CreatePropertyTest(false, 10, 0))
            {
                Assert.Equal(10, value.ValuePersistent);

                value.Value = 15;

                Assert.Equal(10, value.ValuePersistent);
            }
        }

        [Fact]
        public void PersistentPropertyWithInterfaceCachesValue()
        {
            using (var value = Functions.CreatePropertyTest(false, 0, 0))
            {
                Assert.Equal(value.NativePointer, value.SelfPersistent.NativePointer);
            }
        }
    }
}
