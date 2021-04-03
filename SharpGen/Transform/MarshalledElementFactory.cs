using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Linq;

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

        private void CreateCore(CsMarshalBase csMarshallable)
        {
            var marshallable = (CppMarshallable) csMarshallable.CppElement;

            CsTypeBase publicType = null;
            CsTypeBase marshalType = null;

            var mappingRule = marshallable.Rule;
            var publicTypeName = mappingRule is {MappingType: { } mapType} ? mapType : marshallable.TypeName;

            // If CppType is an array, try first to get the binding for this array
            if (csMarshallable.IsArray)
            {
                publicType = typeRegistry.FindBoundType(publicTypeName + "[" + marshallable.ArrayDimension + "]");

                if (publicType != null)
                {
                    csMarshallable.ArrayDimensionValue = 0;
                    csMarshallable.IsArray = false;
                }
            }

            if (publicType == null)
            {
                void AssignStringTypes(CsFundamentalType charType)
                {
                    publicType = csMarshallable.HasPointer || csMarshallable.IsArray
                                     ? TypeRegistry.String
                                     : charType;
                    if (csMarshallable.IsArray)
                        marshalType = charType;
                }

                switch (publicTypeName)
                {
                    case "char":
                        AssignStringTypes(TypeRegistry.UInt8);
                        break;

                    case "wchar_t":
                        csMarshallable.IsWideChar = true;
                        AssignStringTypes(TypeRegistry.Char);
                        break;

                    default:
                        // Try to get a declared type
                        publicType = typeRegistry.FindBoundType(publicTypeName);
                        if (publicType == null)
                        {
                            logger.Fatal("Unknown type found [{0}]", publicTypeName);
                        }

                        // By default, use the underlying native type as the marshal type
                        // if it differs from the public type.
                        marshalType = typeRegistry.FindBoundType(marshallable.TypeName);
                        if (publicType == marshalType)
                        {
                            marshalType = null;
                        }

                        // Otherwise, get the registered marshal type if one exists
                        marshalType ??= typeRegistry.FindBoundMarshalType(publicTypeName);

                        switch (publicType)
                        {
                            case CsStruct csStruct:
                            {
                                if (!csStruct.IsFullyMapped)
                                    // If a structure was not already parsed, then parse it before going further
                                    RequestStructProcessing?.Invoke(csStruct);

                                if (!csStruct.IsFullyMapped)
                                    // No one tried to map the struct so we can't continue.
                                    logger.Fatal(
                                        $"No struct processor processed {csStruct.QualifiedName}. Cannot continue processing"
                                    );

                                if (csStruct.HasMarshalType && !csMarshallable.HasPointer)
                                    // If referenced structure has a specialized marshalling, then use the structure's built-in marshalling 
                                    marshalType = publicType;

                                break;
                            }
                            case CsEnum:
                                // enums don't need a marshal type. They can always marshal as their underlying type.
                                marshalType = null;
                                break;
                        }

                        break;
                }
            }

            if (publicType.IsWellKnownType(globalNamespace, WellKnownName.PointerSize))
            {
                marshalType = PointerType(mappingRule);
            }

            // Present void* elements as IntPtr. Marshal strings as IntPtr
            if (csMarshallable.HasPointer)
            {
                if (publicType == TypeRegistry.Void)
                    publicType = marshalType = PointerType(mappingRule);
                else if (publicType == TypeRegistry.String)
                    marshalType = TypeRegistry.IntPtr;
            }

            csMarshallable.PublicType = publicType;
            csMarshallable.MarshalType = marshalType ?? publicType;

            csMarshallable.Relations = RelationParser.ParseRelation(mappingRule.Relation, logger);
        }

        private static CsFundamentalType PointerType(MappingRule mappingRule) => mappingRule.KeepPointers != true
            ? TypeRegistry.IntPtr
            : TypeRegistry.VoidPtr;

        public CsReturnValue Create(CppReturnValue cppReturnValue)
        {
            CsReturnValue retVal = new(cppReturnValue);

            CreateCore(retVal);

            MakeGeneralPointersBeIntPtr(retVal);

            if (retVal.PublicType is CsInterface {IsCallback: true} iface)
            {
                retVal.PublicType = iface.GetNativeImplementationOrThis();
            }

            return retVal;
        }

        public CsField Create(CppField cppField, string name)
        {
            CsField field = new(cppField, name);

            CreateCore(field);

            MakeGeneralPointersBeIntPtr(field);

            return field;
        }

        private static void MakeGeneralPointersBeIntPtr(CsMarshalBase csMarshallable)
        {
            if (!csMarshallable.HasPointer)
                return;

            if (csMarshallable is CsReturnValue {PublicType: CsStruct} && !csMarshallable.IsArray)
                return;

            csMarshallable.MarshalType = TypeRegistry.IntPtr;

            if (csMarshallable.IsString || csMarshallable.IsInterface)
                return;

            csMarshallable.PublicType = TypeRegistry.IntPtr;
        }

        public CsParameter Create(CppParameter cppParameter, string name)
        {
            CsParameter param = new(cppParameter, name);
            CreateCore(param);

            var cppAttribute = cppParameter.Attribute;
            var paramRule = cppParameter.Rule;

            var isOptional = (cppAttribute & ParamAttribute.Optional) != 0;

            var publicType = param.PublicType;
            var marshalType = param.MarshalType;

            var parameterAttribute = CsParameterAttribute.In;
            var numIndirections = cppParameter.Pointer?.Count(p => p is '*' or '&') ?? 0;

            if ((cppAttribute & ParamAttribute.Buffer) != 0)
                param.IsArray = true;

            if (param.IsArray)
                param.HasPointer = true;

            // --------------------------------------------------------------------------------
            // Pointer - Handle special cases
            // --------------------------------------------------------------------------------
            if (param.HasPointer)
            {
                // --------------------------------------------------------------------------------
                // Handling Parameter Interface
                // --------------------------------------------------------------------------------
                if (publicType is CsInterface)
                {
                    // Force Interface** to be ParamAttribute.Out when None
                    if (cppAttribute is ParamAttribute.In or ParamAttribute.None)
                    {
                        if (numIndirections == 2)
                        {
                            cppAttribute = ParamAttribute.Out;
                        }
                    }

                    if ((cppAttribute & ParamAttribute.In) != 0 || (cppAttribute & ParamAttribute.InOut) != 0)
                    {
                        parameterAttribute = CsParameterAttribute.In;

                        // Force all array of interface to support null
                        if (param.IsArray) 
                            isOptional = true;
                    }
                    else if ((cppAttribute & ParamAttribute.Out) != 0)
                    {
                        parameterAttribute = CsParameterAttribute.Out;
                    }

                    // There's no way to know how to deallocate native-allocated memory correctly since we don't know what allocator the native memory uses
                    // so we treat triple-pointer indirections as IntPtr
                    if (numIndirections > 2)
                    {
                        marshalType = publicType = TypeRegistry.IntPtr;
                        parameterAttribute = CsParameterAttribute.In;
                        param.IsArray = false;
                    }
                }
                else
                {
                    if ((cppAttribute & ParamAttribute.In) != 0)
                    {
                        var fundamentalType = publicType as CsFundamentalType;
                        parameterAttribute = (fundamentalType?.IsPointerSize ?? false)
                                          || publicType.IsWellKnownType(globalNamespace, WellKnownName.FunctionCallback)
                                                 ? CsParameterAttribute.In
                                                 : CsParameterAttribute.RefIn;
                    }
                    else if ((cppAttribute & ParamAttribute.InOut) != 0)
                    {
                        if (isOptional)
                        {
                            publicType = marshalType = PointerType(paramRule);
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
                    var isUntypedPointer = publicType is CsFundamentalType {IsUntypedPointer: true};
                    if (isUntypedPointer && (cppAttribute & ParamAttribute.Buffer) != 0)
                    {
                        param.IsArray = false;
                        parameterAttribute = CsParameterAttribute.In;
                    }
                    // There's no way to know how to deallocate native-allocated memory correctly since we don't know what allocator the native memory uses
                    // so we treat double-pointer indirections as IntPtr
                    else if (numIndirections > 1 && !isUntypedPointer)
                    {
                        marshalType = publicType = PointerType(paramRule);
                        param.IsArray = false;
                    }
                    else if (publicType is CsFundamentalType {IsString: true} &&
                             (cppAttribute & ParamAttribute.Out) != 0)
                    {
                        publicType = TypeRegistry.IntPtr;
                        parameterAttribute = CsParameterAttribute.In;
                        param.IsArray = false;
                    }
                }
            }

            param.Attribute = parameterAttribute;
            param.PublicType = publicType ?? throw new ArgumentException("Public type cannot be null");
            param.MarshalType = marshalType;
            param.OptionalParameter = isOptional;

            // Force IsString to be only string (due to Buffer attribute)
            if (param.IsString)
                param.IsArray = false;

            if (param.Relations?.OfType<StructSizeRelation>().Any() ?? false)
            {
                logger.Error(LoggingCodes.InvalidRelation, $"Parameter [{cppParameter}] marked with a struct-size relationship");
            }

            return param;
        }
    }
}
