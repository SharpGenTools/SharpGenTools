using System;
using Xunit;

namespace Struct
{
    public class CustomNativeNewTests
    {
        [Fact]
        public void CustomNativeNewCalledForParameter()
        {
            CustomNativeNew.NativeNewCalled = false;
            var test = new CustomNativeNew();
            Functions.CustomNativeNewTest(test);
            Assert.True(CustomNativeNew.NativeNewCalled);
            CustomNativeNew.NativeNewCalled = false;
        }

        [Fact]
        public void CustomNativeNewCalledForNestedStruct()
        {
            CustomNativeNew.NativeNewCalled = false;
            var test = new CustomNativeNewNested();

            var native = new CustomNativeNewNested.__Native();

            test.__MarshalTo(ref native);

            Assert.True(CustomNativeNew.NativeNewCalled);
            CustomNativeNew.NativeNewCalled = false;
        }
    }

    public partial struct CustomNativeNew
    {
        public static bool NativeNewCalled { get; set; }

        internal static CustomNativeNew.__Native __NewNative()
        {
            NativeNewCalled = true;
            return new CustomNativeNew.__Native();
        }
    }
}
