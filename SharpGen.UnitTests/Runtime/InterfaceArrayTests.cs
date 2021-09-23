using System;
using SharpGen.Runtime;
using Xunit;

namespace SharpGen.UnitTests.Runtime
{
    public class InterfaceArrayTests
    {
        [Fact]
        public void CreateEmptyFromEmptyArray()
        {
            using var array = new InterfaceArray<CppObject>();

            Assert.Empty(array);
            Assert.Equal(0, array.Length);
        }

        [Fact]
        public void CreateEmptyFromNullArray()
        {
            using var array = new InterfaceArray<CppObject>(null);

            Assert.Empty(array);
            Assert.Equal(0, array.Length);
        }

        [Fact]
        public void CreateEmptyFromSize()
        {
            using var array = new InterfaceArray<CppObject>(0);

            Assert.Empty(array);
            Assert.Equal(0, array.Length);
        }

        private static CppObject NewCppObjectInstance => new(new IntPtr(1));

        [Fact]
        public void CreateFromArray()
        {
            using var cppObject = NewCppObjectInstance;
            using var array = new InterfaceArray<CppObject>(cppObject);

            Assert.NotEmpty(array);
            Assert.Equal(1, array.Length);
            Assert.Equal(cppObject, array[0]);
        }

        [Fact]
        public void CreateFromArrayAndSet()
        {
            using var cppObject = NewCppObjectInstance;
            using var array = new InterfaceArray<CppObject>(new CppObject[] {null});

            Assert.NotEmpty(array);
            Assert.Equal(1, array.Length);
            Assert.Null(array[0]);

            array[0] = cppObject;

            Assert.NotEmpty(array);
            Assert.Equal(1, array.Length);
            Assert.Equal(cppObject, array[0]);
        }

        [Fact]
        public void CreateFromSizeAndSet()
        {
            using var cppObject = NewCppObjectInstance;
            using var array = new InterfaceArray<CppObject>(1);

            Assert.NotEmpty(array);
            Assert.Equal(1, array.Length);
            Assert.Null(array[0]);

            array[0] = cppObject;

            Assert.NotEmpty(array);
            Assert.Equal(1, array.Length);
            Assert.Equal(cppObject, array[0]);
        }
    }
}