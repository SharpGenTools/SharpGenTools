using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    public class MarshalledElementFactory
    {
        private readonly Logger logger;
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly TypeRegistry typeRegistry;

        public MarshalledElementFactory(Logger logger, GlobalNamespaceProvider globalNamespace, TypeRegistry typeRegistry)
        {
            this.typeRegistry = typeRegistry;
            this.globalNamespace = globalNamespace;
            this.logger = logger;
        }

        public event Action<CsStruct> RequestStructProcessing;

        /// <summary>
        /// Gets the C# type from a C++ type.
        /// </summary>
        /// <typeparam name="T">The C# type to return</typeparam>
        /// <param name="marshallable">The marshallable element to create the C# type from.</param>
        /// <returns>An instantiated C# type</returns>
        private T CreateCore<T>(CppMarshallable marshallable) where T : CsMarshalBase, new()
        {
            CsTypeBase publicType = null;
            CsTypeBase marshalType = null;
            var csMarshallable = new T
            {
                CppElement = marshallable,
                IsArray = marshallable.IsArray,
                ArrayDimension = marshallable.ArrayDimension,
                // TODO: handle multidimension
                HasPointer = !string.IsNullOrEmpty(marshallable.Pointer) && (marshallable.Pointer.Contains("*") || marshallable.Pointer.Contains("&")),
            };

            // Calculate ArrayDimension
            int arrayDimensionValue = 0;
            if (marshallable.IsArray)
            {
                if (string.IsNullOrEmpty(marshallable.ArrayDimension))
                    arrayDimensionValue = 0;
                else if (!int.TryParse(marshallable.ArrayDimension, out arrayDimensionValue))
                    arrayDimensionValue = 1;
            }

            // If array Dimension is 0, then it is not an array
            if (arrayDimensionValue == 0)
            {
                marshallable.IsArray = false;
                csMarshallable.IsArray = false;
            }
            csMarshallable.ArrayDimensionValue = arrayDimensionValue;

            string publicTypeName = marshallable.GetTypeNameWithMapping();

            switch (publicTypeName)
            {
                case "char":
                    publicType = typeRegistry.ImportType(typeof(byte));
                    if (csMarshallable.HasPointer)
                        publicType = typeRegistry.ImportType(typeof(string));
                    if (csMarshallable.IsArray)
                    {
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(byte));
                    }
                    break;
                case "wchar_t":
                    publicType = typeRegistry.ImportType(typeof(char));
                    csMarshallable.IsWideChar = true;
                    if (csMarshallable.HasPointer)
                        publicType = typeRegistry.ImportType(typeof(string));
                    if (csMarshallable.IsArray)
                    {
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(char));
                    }
                    break;
                default:

                    // If CppType is an array, try first to get the binding for this array
                    if (marshallable.IsArray)
                        publicType = typeRegistry.FindBoundType(publicTypeName + "[" + marshallable.ArrayDimension + "]");

                    // Else get the typeName
                    if (publicType == null)
                    {
                        // Try to get a declared struct
                        // If it fails, then this struct is unknown
                        publicType = typeRegistry.FindBoundType(publicTypeName);
                        if (publicType == null)
                        {
                            logger.Fatal("Unknown type found [{0}]", publicTypeName);
                        }
                    }
                    else
                    {
                        csMarshallable.ArrayDimensionValue = 0;
                        csMarshallable.IsArray = false;
                    }

                    // By default, use the underlying native type as the marshal type
                    // if it differs from the public type.
                    marshalType = typeRegistry.FindBoundType(marshallable.TypeName);
                    if (publicType == marshalType)
                    {
                        marshalType = null;
                    }

                    if (marshalType == null)
                    {
                        // Otherwise, get the registered marshal type if one exists
                        marshalType = typeRegistry.FindBoundMarshalType(publicTypeName);
                    }

                    if (publicType is CsStruct csStruct)
                    {
                        // If a structure was not already parsed, then parse it before going further
                        if (!csStruct.IsFullyMapped)
                        {
                            RequestStructProcessing?.Invoke(csStruct);
                        }

                        if (!csStruct.IsFullyMapped) // No one tried to map the struct so we can't continue.
                        {
                            logger.Fatal($"No struct processor processed {csStruct.QualifiedName}. Cannot continue processing");
                        }
                        
                        // If referenced structure has a specialized marshalling, then specify marshalling
                        if (csStruct.HasMarshalType && !csMarshallable.HasPointer)
                        {
                            marshalType = publicType;
                        }
                    }
                    else if (publicType is CsEnum referenceEnum)
                    {
                        marshalType = null; // enums don't need a marshal type. They can always marshal as their underlying type.
                    }
                    break;
            }

            // Set bool to int conversion case
            csMarshallable.IsBoolToInt = 
                marshalType is CsFundamentalType marshalFundamental && IsIntegerFundamentalType(marshalFundamental)
                && publicType is CsFundamentalType publicFundamental && publicFundamental.Type == typeof(bool);

            // Default IntPtr type for pointer, unless modified by specialized type (like char* map to string)
            if (csMarshallable.HasPointer)
            {
                marshalType = typeRegistry.ImportType(typeof(IntPtr));
                
                if (publicTypeName == "void")
                {
                    publicType = typeRegistry.ImportType(typeof(IntPtr));
                }

                switch (publicTypeName)
                {
                    case "char":
                        publicType = typeRegistry.ImportType(typeof(string));
                        break;
                    case "wchar_t":
                        publicType = typeRegistry.ImportType(typeof(string));
                        csMarshallable.IsWideChar = true;
                        break;
                }
            }

            csMarshallable.PublicType = publicType;
            csMarshallable.MarshalType = marshalType ?? publicType;

            return csMarshallable;
        }

        private static bool IsIntegerFundamentalType(CsFundamentalType marshalType)
        {
            return marshalType.Type == typeof(int)
                || marshalType.Type == typeof(short)
                || marshalType.Type == typeof(byte)
                || marshalType.Type == typeof(long)
                || marshalType.Type == typeof(uint)
                || marshalType.Type == typeof(ushort)
                || marshalType.Type == typeof(sbyte)
                || marshalType.Type == typeof(ulong);
        }

        public CsMarshalBase Create(CppMarshallable cppMarshallable)
        {
            return CreateCore<CsMarshalBase>(cppMarshallable);
        }

        public CsReturnValue Create(CppReturnValue cppReturnValue)
        {
            return CreateCore<CsReturnValue>(cppReturnValue);
        }

        public CsField Create(CppField cppField)
        {
            var field = CreateCore<CsField>(cppField);

            if (field.HasPointer && field.PublicType != typeRegistry.ImportType(typeof(string)))
            {
                field.PublicType = typeRegistry.ImportType(typeof(IntPtr));
            }
            else if (field.PublicType.QualifiedName == globalNamespace.GetTypeName(WellKnownName.PointerSize))
            {
                // Special case for Size type, as it is default marshal to IntPtr for method parameter
                field.MarshalType = field.PublicType;
            }
            return field;
        }

        public CsParameter Create(CppParameter cppParameter)
        {
            var param = CreateCore<CsParameter>(cppParameter);

            var cppAttribute = cppParameter.Attribute;
            var paramRule = cppParameter.GetMappingRule();

            bool hasArray = cppParameter.IsArray || ((cppAttribute & ParamAttribute.Buffer) != 0);
            bool hasParams = (cppAttribute & ParamAttribute.Params) == ParamAttribute.Params;
            bool isOptional = (cppAttribute & ParamAttribute.Optional) != 0;
            bool hasPointer = param.HasPointer;

            var publicType = param.PublicType;
            var marshalType = param.MarshalType;

            CsParameterAttribute parameterAttribute = CsParameterAttribute.In;

            if (hasArray)
            {
                hasPointer = true;
            }

            // --------------------------------------------------------------------------------
            // Pointer - Handle special cases
            // --------------------------------------------------------------------------------
            if (hasPointer)
            {
                marshalType = typeRegistry.ImportType(typeof(IntPtr));

                // --------------------------------------------------------------------------------
                // Handling Parameter Interface
                // --------------------------------------------------------------------------------
                if (publicType is CsInterface publicInterface)
                {
                    // Force Interface** to be ParamAttribute.Out when None
                    if (cppAttribute == ParamAttribute.In)
                    {
                        if (cppParameter.Pointer == "**")
                            cppAttribute = ParamAttribute.Out;
                    }

                    if ((cppAttribute & ParamAttribute.In) != 0 || (cppAttribute & ParamAttribute.InOut) != 0)
                    {
                        parameterAttribute = CsParameterAttribute.In;

                        // Force all array of interface to support null
                        if (hasArray)
                        {
                            isOptional = true;
                        }
                    }
                    else if ((cppAttribute & ParamAttribute.Out) != 0)
                        parameterAttribute = CsParameterAttribute.Out;
                }
                else
                {
                    // If a pointer to array of bool are handle as array of int
                    if (param.IsBoolToInt && (cppAttribute & ParamAttribute.Buffer) != 0)
                        publicType = typeRegistry.ImportType(typeof(int));

                    if ((cppAttribute & ParamAttribute.In) != 0)
                    {
                        var fundamentalType = (publicType as CsFundamentalType)?.Type;
                        parameterAttribute = fundamentalType == typeof(IntPtr)
                                            || publicType.Name == globalNamespace.GetTypeName(WellKnownName.FunctionCallback)
                                            || fundamentalType == typeof(string)
                                                    ? CsParameterAttribute.In
                                                    : CsParameterAttribute.RefIn;
                    }
                    else if ((cppAttribute & ParamAttribute.InOut) != 0)
                    {
                        if ((cppAttribute & ParamAttribute.Optional) != 0)
                        {
                            publicType = typeRegistry.ImportType(typeof(IntPtr));
                            parameterAttribute = CsParameterAttribute.In;
                        }
                        else
                        {
                            parameterAttribute = CsParameterAttribute.Ref;
                        }

                    }
                    else if ((cppAttribute & ParamAttribute.Out) != 0)
                        parameterAttribute = CsParameterAttribute.Out;

                    // Handle void* with Buffer attribute
                    if (cppParameter.GetTypeNameWithMapping() == "void" && (cppAttribute & ParamAttribute.Buffer) != 0)
                    {
                        hasArray = false;
                        parameterAttribute = CsParameterAttribute.In;
                    }
                    else if (publicType is CsFundamentalType fundamental && fundamental.Type == typeof(string)
                        && (cppAttribute & ParamAttribute.Out) != 0)
                    {
                        publicType = typeRegistry.ImportType(typeof(IntPtr));
                        parameterAttribute = CsParameterAttribute.In;
                        hasArray = false;
                    }
                    else if (publicType is CsStruct structType &&
                                (parameterAttribute == CsParameterAttribute.Out || hasArray || parameterAttribute == CsParameterAttribute.RefIn || parameterAttribute == CsParameterAttribute.Ref))
                    {
                        // Set MarshalledToNative on structure to generate proper marshalling
                        structType.MarshalledToNative = true;
                    }
                }
            }
            else if (publicType is CsStruct structType && parameterAttribute != CsParameterAttribute.Out)
            {
                structType.MarshalledToNative = true;
            }

            param.HasPointer = hasPointer;
            param.Attribute = parameterAttribute;
            param.IsArray = hasArray;
            param.HasParams = hasParams;
            param.HasPointer = hasPointer;
            param.PublicType = publicType ?? throw new ArgumentException("Public type cannot be null");
            param.MarshalType = marshalType;
            param.IsOptional = isOptional;

            // Force IsString to be only string (due to Buffer attribute)
            if (param.IsString)
            {
                param.IsArray = false;
            }
            return param;
        }
    }
}
