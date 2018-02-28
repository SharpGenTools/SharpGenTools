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

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csMarshallable.MarshalType);
        }

        [Fact]
        public void ParamWithNoTypeMappingShouldHaveMarshalTypeEqualToPublic()
        {
            var marshallable = new CppMarshallable
            {
                TypeName = "int"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(marshallable);

            Assert.Equal(csMarshallable.PublicType, csMarshallable.MarshalType);
        }

        [Fact]
        public void DoublePointerParameterMappedAsOut()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "Interface",
                Pointer = "**"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("Interface", new CsInterface {Name = "Interface"});

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(CsParameterAttribute.Out, csParameter.Attribute);
        }

        [Fact]
        public void CharArrayMappedToStringMarshalledWithByte()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "char",
                IsArray = true,
                ArrayDimension = "10"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(string)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(byte)), csMarshallable.MarshalType);
            Assert.Equal(10, csMarshallable.ArrayDimensionValue);
            Assert.True(csMarshallable.IsArray);
        }

        [Fact]
        public void WCharArrayMappedToStringMarshalledWithByte()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "wchar_t",
                IsArray = true,
                ArrayDimension = "10"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(string)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(char)), csMarshallable.MarshalType);
            Assert.Equal(10, csMarshallable.ArrayDimensionValue);
            Assert.True(csMarshallable.IsArray);
            Assert.True(csMarshallable.IsWideChar);
        }

        

        [Fact]
        public void CharPointerMappedToStringMarshalledWithIntPtr()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "char",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(string)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csMarshallable.MarshalType);
            Assert.True(csMarshallable.HasPointer);
        }

        [Fact]
        public void WCharPointerMappedToStringMarshalledWithIntPtr()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "wchar_t",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(string)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csMarshallable.MarshalType);
            Assert.True(csMarshallable.HasPointer);
            Assert.True(csMarshallable.IsWideChar);
        }

        [Fact]
        public void ZeroDimensionArrayMappedAsSingleElement()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "int",
                ArrayDimension = "0",
                IsArray = true
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.False(csMarshallable.IsArray);
        }

        [Fact]
        public void MappedTypeOverridesPublicTypeNotMarshalType()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "int",
                ArrayDimension = "0",
                IsArray = true,
                Rule = new MappingRule
                {
                    MappingType = "bool"
                }
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)));
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(bool)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(int)), csMarshallable.MarshalType);
        }

        [Fact]
        public void NativeTypeTakesPrecedenceOverMarshalTypeForMappedType()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "short",
                ArrayDimension = "0",
                IsArray = true,
                Rule = new MappingRule
                {
                    MappingType = "bool"
                }
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(int)));
            typeRegistry.BindType("short", typeRegistry.ImportType(typeof(short)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(bool)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(short)), csMarshallable.MarshalType);
        }

        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [Theory]
        public void BoolToIntSetForAllIntegerTypes(Type integerType)
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "Integer",
                ArrayDimension = "0",
                IsArray = true,
                Rule = new MappingRule
                {
                    MappingType = "bool"
                }
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)));
            typeRegistry.BindType("Integer", typeRegistry.ImportType(integerType));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);

            Assert.Equal(typeRegistry.ImportType(typeof(bool)), csMarshallable.PublicType);
            Assert.Equal(typeRegistry.ImportType(integerType), csMarshallable.MarshalType);
            Assert.True(csMarshallable.IsBoolToInt);
        }

        [Fact]
        public void FieldWithPointerTypeMarshalledAsIntPtr()
        {
            var cppField = new CppField
            {
                TypeName = "Interface",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("Interface", new CsInterface { Name = "Interface" });
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csField = marshalledElementFactory.Create(cppField);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csField.PublicType);
            Assert.Equal(csField.PublicType, csField.MarshalType);
        }

        [Fact]
        public void PointerParameterMarshalTypeIsIntPtr()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "int",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, new DocumentationLinker());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider("SharpGen.Runtime"), typeRegistry);

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.MarshalType);
        }
    }
}
