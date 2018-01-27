using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class MarshalledElementFactoryTests : TestBase
    {
        public MarshalledElementFactoryTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void PointerToTypeWithMarshallingMarshalsAsIntPtr()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "bool",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create<CsMarshalBase>(cppMarshallable);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csMarshallable.MarshalType);
        }
    }
}
