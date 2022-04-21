using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests;

public class MarshalledElementFactoryTests : TestBase
{
    private readonly MarshalledElementFactory marshalledElementFactory;

    public MarshalledElementFactoryTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        marshalledElementFactory = new MarshalledElementFactory(Ioc);
    }

    [Fact]
    public void PointerToTypeWithMarshallingMarshalsAsUnderlyingType()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "bool",
            Pointer = "*"
        };

        TypeRegistry.BindType("bool", TypeRegistry.Boolean, TypeRegistry.Int32);

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);
        Assert.Equal(TypeRegistry.Int32, csMarshallable.MarshalType);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
        Assert.False(csMarshallable.IsArray);
        Assert.True(csMarshallable.HasPointer);
    }

    [Fact]
    public void ParamWithNoTypeMappingShouldHaveMarshalTypeEqualToPublic()
    {
        var marshallable = new CppParameter("param")
        {
            TypeName = "int"
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);

        var csMarshallable = marshalledElementFactory.Create(marshallable, marshallable.Name);

        Assert.Equal(csMarshallable.PublicType, csMarshallable.MarshalType);
    }

    [Fact]
    public void DoublePointerParameterMappedAsOut()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "Interface",
            Pointer = "**"
        };

        TypeRegistry.BindType("Interface", new CsInterface(null, "Interface"));

        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.Equal(CsParameterAttribute.Out, csParameter.Attribute);
    }

    [Fact]
    public void CharArrayMappedToStringMarshalledWithByte()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "char",
            ArrayDimension = "10"
        };

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.String, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.UInt8, csMarshallable.MarshalType);
        Assert.Equal(10, csMarshallable.ArrayDimensionValue);
        Assert.False(csMarshallable.IsArray);
        Assert.True(csMarshallable.IsString);
        Assert.False(csMarshallable.IsWideChar);
    }

    [Fact]
    public void WCharArrayMappedToStringMarshalledWithByte()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "wchar_t",
            ArrayDimension = "10"
        };

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.String, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.Char, csMarshallable.MarshalType);
        Assert.Equal(10, csMarshallable.ArrayDimensionValue);
        Assert.False(csMarshallable.IsArray);
        Assert.True(csMarshallable.HasPointer);
        Assert.True(csMarshallable.IsString);
        Assert.True(csMarshallable.IsWideChar);
    }

    [Fact]
    public void CharPointerMappedToStringMarshalledWithIntPtr()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "char",
            Pointer = "*"
        };

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.String, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csMarshallable.MarshalType);
        Assert.True(csMarshallable.HasPointer);
        Assert.False(csMarshallable.IsArray);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [Fact]
    public void WCharPointerMappedToStringMarshalledWithIntPtr()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "wchar_t",
            Pointer = "*"
        };

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.String, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csMarshallable.MarshalType);
        Assert.False(csMarshallable.IsArray);
        Assert.True(csMarshallable.HasPointer);
        Assert.True(csMarshallable.IsString);
        Assert.True(csMarshallable.IsWideChar);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [Fact]
    public void ZeroDimensionArrayMappedAsSingleElement()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "int",
            ArrayDimension = "0"
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);
        Assert.Equal(TypeRegistry.Int32, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.Int32, csMarshallable.MarshalType);
        Assert.False(csMarshallable.IsArray);
        Assert.False(csMarshallable.HasPointer);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [Fact]
    public void MappedTypeOverridesPublicTypeNotMarshalType()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "int",
            ArrayDimension = "0",
            Rule =
            {
                MappingType = "bool"
            }
        };

        TypeRegistry.BindType("bool", TypeRegistry.Boolean);
        TypeRegistry.BindType("int", TypeRegistry.Int32);

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.Boolean, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.Int32, csMarshallable.MarshalType);
        Assert.False(csMarshallable.HasPointer);
        Assert.False(csMarshallable.IsArray);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [Fact]
    public void NativeTypeTakesPrecedenceOverMarshalTypeForMappedType()
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "short",
            ArrayDimension = "0",
            Rule =
            {
                MappingType = "bool"
            }
        };

        TypeRegistry.BindType("bool", TypeRegistry.Boolean, TypeRegistry.Int32);
        TypeRegistry.BindType("short", TypeRegistry.Int16);

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.Boolean, csMarshallable.PublicType);
        Assert.Equal(TypeRegistry.Int16, csMarshallable.MarshalType);
        Assert.False(csMarshallable.HasPointer);
        Assert.False(csMarshallable.IsArray);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [InlineData(PrimitiveTypeCode.UInt8)]
    [InlineData(PrimitiveTypeCode.Int8)]
    [InlineData(PrimitiveTypeCode.UInt16)]
    [InlineData(PrimitiveTypeCode.Int16)]
    [InlineData(PrimitiveTypeCode.UInt32)]
    [InlineData(PrimitiveTypeCode.Int32)]
    [InlineData(PrimitiveTypeCode.UInt64)]
    [InlineData(PrimitiveTypeCode.Int64)]
    [Theory]
    public void BoolToIntSetForAllIntegerTypes(PrimitiveTypeCode integerType)
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = "Integer",
            ArrayDimension = "0",
            Rule =
            {
                MappingType = "bool"
            }
        };

        var integerPrimitiveType = TypeRegistry.ImportPrimitiveType(integerType);

        TypeRegistry.BindType("bool", TypeRegistry.Boolean);
        TypeRegistry.BindType("Integer", integerPrimitiveType);


        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(TypeRegistry.Boolean, csMarshallable.PublicType);
        Assert.Equal(integerPrimitiveType, csMarshallable.MarshalType);
        Assert.True(csMarshallable.IsBoolToInt);
        Assert.False(csMarshallable.HasPointer);
        Assert.False(csMarshallable.IsArray);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
    }

    [Fact]
    public void FieldWithPointerTypeMarshalledAsIntPtr()
    {
        var cppField = new CppField("field")
        {
            TypeName = "int",
            Pointer = "*"
        };

        TypeRegistry.BindType("int", TypeRegistry.IntPtr);

        var csField = marshalledElementFactory.Create(cppField, cppField.Name);

        Assert.Equal(TypeRegistry.IntPtr, csField.PublicType);
        Assert.Equal(csField.PublicType, csField.MarshalType);
    }

    [Fact]
    public void FieldWithPointerToInterfaceTypeHasPublicTypeOfInterface()
    {
        var cppField = new CppField("field")
        {
            TypeName = "Interface",
            Pointer = "*"
        };

        TypeRegistry.BindType("Interface", new CsInterface(null, "Interface"));

        var csField = marshalledElementFactory.Create(cppField, cppField.Name);

        Assert.Equal(TypeRegistry.FindBoundType("Interface"), csField.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csField.MarshalType);
        Assert.True(csField.IsInterface);
    }

    [Fact]
    public void PointerParameterMarkedAsHasPointer()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "int",
            Pointer = "*"
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);

        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.False(csParameter.IsArray);
        Assert.False(csParameter.IsString);
        Assert.True(csParameter.HasPointer);
        Assert.Equal(0, csParameter.ArrayDimensionValue);
    }

    [Fact]
    public void BoolToIntArray()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "bool", Pointer = "*",
            Attribute = ParamAttribute.In | ParamAttribute.Buffer
        };

        TypeRegistry.BindType("bool", TypeRegistry.Boolean, TypeRegistry.UInt8);

        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.Equal(TypeRegistry.Boolean, csParameter.PublicType);
        Assert.Equal(TypeRegistry.UInt8, csParameter.MarshalType);
        Assert.True(csParameter.IsArray);
        Assert.True(csParameter.IsBoolToInt);
        Assert.Equal(0, csParameter.ArrayDimensionValue);
    }

    [Fact]
    public void ParameterWithStructSizeRelationLogsError()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "int",
            Rule =
            {
                Relation = "struct-size()"
            }
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);

        using (LoggerMessageCountEnvironment(1, LogLevel.Error))
        using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
        using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
        {
            marshalledElementFactory.Create(cppParameter, cppParameter.Name);
        }
    }

    [Fact]
    public void DoublePointerNonInterfaceParameterMappedAsIntPtr()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "int",
            Pointer = "**"
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);
        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.Equal(TypeRegistry.IntPtr, csParameter.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csParameter.MarshalType);
        Assert.True(csParameter.HasPointer);
        Assert.False(csParameter.IsArray);
        Assert.Equal(0, csParameter.ArrayDimensionValue);
    }

    [Fact]
    public void DoubleVoidPointerParameterPreserved()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "void",
            Pointer = "**",
            Attribute = ParamAttribute.Out
        };

        TypeRegistry.BindType("void", TypeRegistry.Void);
        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.Equal(TypeRegistry.IntPtr, csParameter.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csParameter.MarshalType);
        Assert.True(csParameter.HasPointer);
        Assert.False(csParameter.IsArray);
        Assert.True(csParameter.IsOut);
        Assert.Equal(0, csParameter.ArrayDimensionValue);
    }

    [Fact]
    public void PointerNonInterfaceReturnValueMappedAsIntPtr()
    {
        var cppReturnValue = new CppReturnValue
        {
            TypeName = "int",
            Pointer = "*"
        };

        TypeRegistry.BindType("int", TypeRegistry.Int32);
        var csReturnValue = marshalledElementFactory.Create(cppReturnValue);

        Assert.Equal(TypeRegistry.IntPtr, csReturnValue.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csReturnValue.MarshalType);
        Assert.True(csReturnValue.HasPointer);
        Assert.False(csReturnValue.IsArray);
        Assert.Equal(0, csReturnValue.ArrayDimensionValue);
    }


    [Fact]
    public void TriplePointerInterfaceParameterMappedAsIntPtr()
    {
        var cppParameter = new CppParameter("param")
        {
            TypeName = "Interface",
            Pointer = "***"
        };

        TypeRegistry.BindType("Interface", new CsInterface(null, "Interface"));

        var csParameter = marshalledElementFactory.Create(cppParameter, cppParameter.Name);

        Assert.Equal(TypeRegistry.IntPtr, csParameter.PublicType);
        Assert.Equal(TypeRegistry.IntPtr, csParameter.MarshalType);
        Assert.True(csParameter.HasPointer);
        Assert.False(csParameter.IsArray);
        Assert.Equal(0, csParameter.ArrayDimensionValue);
    }

    [InlineData("TCHAR")]
    [InlineData("char")]
    [InlineData("wchar_t")]
    [Theory]
    public void BoundTypeForArrayType(string elementType)
    {
        var cppMarshallable = new CppParameter("param")
        {
            TypeName = elementType,
            ArrayDimension = "1" // ANYSIZE_ARRAY
        };

        CsInterface dynamicStringType = new(null, "DynamicString");
        TypeRegistry.BindType(elementType + "[1]", dynamicStringType);

        var csMarshallable = marshalledElementFactory.Create(cppMarshallable, cppMarshallable.Name);

        Assert.Equal(dynamicStringType, csMarshallable.PublicType);
        Assert.Equal(dynamicStringType, csMarshallable.MarshalType);
        Assert.Equal(0, csMarshallable.ArrayDimensionValue);
        Assert.False(csMarshallable.IsArray);
        Assert.False(csMarshallable.IsString);
        Assert.False(csMarshallable.IsWideChar);
    }
}