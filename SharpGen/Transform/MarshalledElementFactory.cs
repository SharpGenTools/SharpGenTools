using System;
using System.Diagnostics;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Transform
{
    public sealed class MarshalledElementFactory
    {
        private readonly Ioc ioc;

        private Logger Logger => ioc.Logger;
        private GlobalNamespaceProvider GlobalNamespace => ioc.GlobalNamespace;
        private TypeRegistry TypeRegistry => ioc.TypeRegistry;

        public MarshalledElementFactory(Ioc ioc)
        {
            this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
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
                publicType = TypeRegistry.FindBoundType(publicTypeName + "[" + marshallable.ArrayDimension + "]");

                if (publicType != null)
                    csMarshallable.IsArray = false;
            }

            if (publicType == null)
            {
                void AssignStringTypes(CsFundamentalType charType)
                {
                    publicType = csMarshallable.HasPointer || csMarshallable.IsArray
                                     ? TypeRegistry.String
                                     : charType;
                    marshalType = csMarshallable.IsArray ? charType : null;
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
                        if (!TypeRegistry.FindBoundType(publicTypeName, out var boundType))
                        {
                            Logger.Fatal("Unknown type found [{0}]", publicTypeName);
                            return;
                        }

                        publicType = boundType.CSharpType;

                        // By default, use the underlying native type as the marshal type
                        // if it differs from the public type.
                        marshalType = TypeRegistry.FindBoundType(marshallable.TypeName);
                        if (publicType == marshalType)
                            marshalType = null;

                        // Otherwise, get the registered marshal type if one exists
                        marshalType ??= boundType.MarshalType;

                        break;
                }
            }

            switch (publicType)
            {
                case CsStruct csStruct:
                {
                    if (!csStruct.IsFullyMapped)
                        // If a structure was not already parsed, then parse it before going further
                        RequestStructProcessing?.Invoke(csStruct);

                    if (!csStruct.IsFullyMapped)
                        // No one tried to map the struct so we can't continue.
                        Logger.Fatal(
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

            if (publicType.IsWellKnownType(GlobalNamespace, WellKnownName.PointerSize))
                marshalType = PointerType(mappingRule);

            // Present void* elements as IntPtr. Marshal strings as IntPtr
            if (marshallable.HasPointer)
            {
                if (publicType == TypeRegistry.Void)
                    publicType = marshalType = PointerType(mappingRule);
                else if (publicType == TypeRegistry.String)
                    marshalType = TypeRegistry.IntPtr;
            }

            csMarshallable.PublicType = publicType;
            csMarshallable.MarshalType = marshalType;

            csMarshallable.Relations = RelationParser.ParseRelation(mappingRule.Relation, Logger);
        }

        private static CsFundamentalType PointerType(MappingRule mappingRule) => mappingRule.KeepPointers != true
            ? TypeRegistry.IntPtr
            : TypeRegistry.VoidPtr;

        public CsReturnValue Create(CppReturnValue cppReturnValue)
        {
            CsReturnValue retVal = new(ioc, cppReturnValue);

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
            CsField field = new(ioc, cppField, name);

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
            CsParameter param = new(ioc, cppParameter, name);
            CreateCore(param);

            if (cppParameter.IsAttributeRuleRedundant)
                Logger.Message("Parameter [{0}] has redundant attribute rule specification", cppParameter.FullName);

            static bool HasFlag(ParamAttribute value, ParamAttribute flag) => (value & flag) == flag;

            // --------------------------------------------------------------------------------
            // Pointer - Handle special cases
            // --------------------------------------------------------------------------------
            if (param.HasPointer)
            {
                var paramRule = cppParameter.Rule;
                var numIndirections = cppParameter.Pointer.Count(static p => p is '*' or '&');
                bool isBuffer, isIn, isInOut, isOut;

                {
                    var cppAttribute = cppParameter.Attribute;

                    // Force Interface** to be ParamAttribute.Out
                    if (param.PublicType is CsInterface && cppAttribute == ParamAttribute.In && numIndirections == 2)
                        cppAttribute = ParamAttribute.Out;

                    isBuffer = HasFlag(cppAttribute, ParamAttribute.Buffer);
                    isIn = HasFlag(cppAttribute, ParamAttribute.In);
                    isInOut = HasFlag(cppAttribute, ParamAttribute.InOut);
                    isOut = HasFlag(cppAttribute, ParamAttribute.Out);
                }

                // Either In, InOut or Out is set
                Debug.Assert((isIn ? 1 : 0) + (isInOut ? 1 : 0) + (isOut ? 1 : 0) == 1);

                // --------------------------------------------------------------------------------
                // Handling Parameter Interface
                // --------------------------------------------------------------------------------
                if (param.PublicType is CsInterface)
                {
                    // Simplify logic by assuming interface instance pointer is the interface itself.
                    --numIndirections;

                    if (isOut)
                        param.Attribute = CsParameterAttribute.Out;
                }
                else if (isIn)
                {
                    var publicType = param.PublicType;

                    param.Attribute = publicType is CsFundamentalType {IsPointerSize: true}
                                   || publicType.IsWellKnownType(GlobalNamespace, WellKnownName.FunctionCallback)
                                          ? CsParameterAttribute.In
                                          : CsParameterAttribute.RefIn;
                }
                else if (isInOut)
                {
                    if (param.IsOptional)
                    {
                        param.SetPublicResetMarshalType(PointerType(paramRule));
                        param.Attribute = CsParameterAttribute.In;
                    }
                    else
                    {
                        param.Attribute = CsParameterAttribute.Ref;
                    }
                }
                else if (isOut)
                {
                    param.Attribute = CsParameterAttribute.Out;
                }

                switch (param.PublicType)
                {
                    // Handle void* with Buffer attribute
                    case CsFundamentalType {IsUntypedPointer: true} when isBuffer:
                        param.Attribute = CsParameterAttribute.In;
                        param.IsArray = false;
                        break;
                    // Handle strings with Out attribute
                    case CsFundamentalType {IsString: true} when isOut:
                        param.Attribute = CsParameterAttribute.In;
                        param.IsArray = false;
                        param.SetPublicResetMarshalType(TypeRegistry.IntPtr);
                        break;
                    // There's no way to know how to deallocate native-allocated memory correctly
                    // since we don't know what allocator the native memory uses,
                    // so we treat any extra pointer indirections as IntPtr
                    case not CsFundamentalType {IsUntypedPointer: true} when numIndirections > 1:
                        param.IsArray = false;
                        param.SetPublicResetMarshalType(TypeRegistry.IntPtr);
                        break;
                }
            }

            if (param.Relations.OfType<StructSizeRelation>().Any())
                Logger.Error(
                    LoggingCodes.InvalidRelation,
                    $"Parameter [{cppParameter}] marked with a struct-size relationship"
                );

            return param;
        }
    }
}
