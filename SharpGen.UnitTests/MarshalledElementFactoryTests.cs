using System;
using FakeItEasy;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
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
        public void PointerToTypeWithMarshallingMarshalsAsUnderlyingType()
        {
            var cppMarshallable = new CppMarshallable
            {
                TypeName = "bool",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            var csMarshallable = marshalledElementFactory.Create(cppMarshallable);
            Assert.Equal(typeRegistry.ImportType(typeof(int)), csMarshallable.MarshalType);
        }

        [Fact]
        public void ParamWithNoTypeMappingShouldHaveMarshalTypeEqualToPublic()
        {
            var marshallable = new CppMarshallable
            {
                TypeName = "int"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("Interface", new CsInterface { Name = "Interface" });

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)));
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(int)));
            typeRegistry.BindType("short", typeRegistry.ImportType(typeof(short)));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)));
            typeRegistry.BindType("Integer", typeRegistry.ImportType(integerType));

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

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
                TypeName = "int",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(IntPtr)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            var csField = marshalledElementFactory.Create(cppField);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csField.PublicType);
            Assert.Equal(csField.PublicType, csField.MarshalType);
        }

        [Fact]
        public void FieldWithPointerToInterfaceTypeHasPublicTypeOfInterface()
        {
            var cppField = new CppField
            {
                TypeName = "Interface",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("Interface", new CsInterface { Name = "Interface" });
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            var csField = marshalledElementFactory.Create(cppField);

            Assert.Equal(typeRegistry.FindBoundType("Interface"), csField.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csField.MarshalType);
            Assert.True(csField.IsInterface);
        }

        [Fact]
        public void PointerParameterMarkedAsHasPointer()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "int",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.True(csParameter.HasPointer);
        }

        [Fact]
        public void BoolToIntArrayPublicTypeIsBoolArray()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "bool",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(byte)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            cppParameter.Attribute = ParamAttribute.In | ParamAttribute.Buffer;

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(bool)), csParameter.PublicType);
            Assert.True(csParameter.IsArray);
            Assert.True(csParameter.IsBoolToInt);
        }

        [Fact]
        public void BoolToIntArrayMarshalTypeIsIntegerArray()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "bool",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("bool", typeRegistry.ImportType(typeof(bool)), typeRegistry.ImportType(typeof(byte)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            cppParameter.Attribute = ParamAttribute.In | ParamAttribute.Buffer;

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(byte)), csParameter.MarshalType);
            Assert.True(csParameter.IsArray);
            Assert.True(csParameter.IsBoolToInt);
        }

        [Fact]
        public void ParameterWithStructSizeRelationLogsError()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "int"
            };

            cppParameter.GetMappingRule().Relation = "struct-size()";

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            using (LoggerMessageCountEnvironment(1, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                marshalledElementFactory.Create(cppParameter);
            }
        }

        [Fact]
        public void DoublePointerNonInterfaceParameterMappedAsIntPtr()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "int",
                Pointer = "**"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);
            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.MarshalType);
            Assert.True(csParameter.HasPointer);
        }

        [Fact]
        public void DoubleVoidPointerParameterPreserved()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "void",
                Pointer = "**",
                Attribute = ParamAttribute.Out
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("void", typeRegistry.ImportType(typeof(void)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);
            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.MarshalType);
            Assert.True(csParameter.HasPointer);
            Assert.True(csParameter.IsOut);
        }

        [Fact]
        public void PointerNonInterfaceReturnValueMappedAsIntPtr()
        {
            var cppReturnValue = new CppReturnValue
            {
                TypeName = "int",
                Pointer = "*"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("int", typeRegistry.ImportType(typeof(int)));
            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);
            var csReturnValue = marshalledElementFactory.Create(cppReturnValue);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csReturnValue.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csReturnValue.MarshalType);
            Assert.True(csReturnValue.HasPointer);
        }


        [Fact]
        public void TriplePointerInterfaceParameterMappedAsIntPtr()
        {
            var cppParameter = new CppParameter
            {
                TypeName = "Interface",
                Pointer = "***"
            };

            var typeRegistry = new TypeRegistry(Logger, A.Fake<IDocumentationLinker>());
            typeRegistry.BindType("Interface", new CsInterface { Name = "Interface" });

            var marshalledElementFactory = new MarshalledElementFactory(Logger, new GlobalNamespaceProvider(), typeRegistry);

            var csParameter = marshalledElementFactory.Create(cppParameter);

            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.PublicType);
            Assert.Equal(typeRegistry.ImportType(typeof(IntPtr)), csParameter.MarshalType);
            Assert.True(csParameter.HasPointer);
        }

    }
}
