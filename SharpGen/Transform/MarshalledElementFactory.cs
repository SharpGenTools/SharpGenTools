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
        /// <param name="isTypeUsedInStruct">if set to <c>true</c> this type is used in a struct declaration.</param>
        /// <returns>An instantiated C# type</returns>
        public T Create<T>(CppMarshallable marshallable, bool isTypeUsedInStruct = false) where T : CsMarshalBase, new()
        {
            CsTypeBase publicType = null;
            CsTypeBase marshalType = null;
            var interopType = new T
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
                interopType.IsArray = false;
            }
            interopType.ArrayDimensionValue = arrayDimensionValue;

            string typeName = marshallable.GetTypeNameWithMapping();

            switch (typeName)
            {
                case "char":
                    publicType = typeRegistry.ImportType(typeof(byte));
                    if (interopType.HasPointer)
                        publicType = typeRegistry.ImportType(typeof(string));
                    if (interopType.IsArray)
                    {
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(byte));
                    }
                    break;
                case "wchar_t":
                    publicType = typeRegistry.ImportType(typeof(char));
                    interopType.IsWideChar = true;
                    if (interopType.HasPointer)
                        publicType = typeRegistry.ImportType(typeof(string));
                    if (interopType.IsArray)
                    {
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(char));
                    }
                    break;
                default:

                    // If CppType is an array, try first to get the binding for this array
                    if (marshallable.IsArray)
                        publicType = typeRegistry.FindBoundType(typeName + "[" + marshallable.ArrayDimension + "]");

                    // Else get the typeName
                    if (publicType == null)
                    {
                        // Try to get a declared struct
                        // If it fails, then this struct is unknown
                        publicType = typeRegistry.FindBoundType(typeName);
                        if (publicType == null)
                        {
                            logger.Fatal("Unknown type found [{0}]", typeName);
                        }
                    }
                    else
                    {
                        interopType.ArrayDimensionValue = 0;
                        interopType.IsArray = false;
                    }

                    // Get a MarshalType if any
                    marshalType = typeRegistry.FindBoundMarshalType(typeName);

                    if (publicType is CsStruct referenceStruct)
                    {
                        // If a structure was not already parsed, then parse it before going further
                        if (!referenceStruct.IsFullyMapped)
                        {
                            RequestStructProcessing?.Invoke(referenceStruct);
                        }

                        if (!referenceStruct.IsFullyMapped) // No one tried to map the struct so we can't continue.
                        {
                            logger.Fatal($"No struct processor processed {referenceStruct.QualifiedName}. Cannot continue processing");
                        }
                        
                        // If referenced structure has a specialized marshalling, then specify marshalling
                        if (referenceStruct.HasMarshalType && !interopType.HasPointer)
                        {
                            marshalType = publicType;
                        }
                    }
                    else if (publicType is CsEnum)
                    {
                        var referenceEnum = publicType as CsEnum;
                        // Fixed array of enum should be mapped to their respective blittable type
                        if (interopType.IsArray)
                        {
                            marshalType = typeRegistry.ImportType(referenceEnum.Type);
                        }
                    }
                    break;
            }

            // Set bool to int conversion case
            interopType.IsBoolToInt = marshalType?.Type == typeof(int) && publicType.Type == typeof(bool);

            // Default IntPtr type for pointer, unless modified by specialized type (like char* map to string)
            if (interopType.HasPointer)
            {
                if (isTypeUsedInStruct)
                {
                    publicType = typeRegistry.ImportType(typeof(IntPtr));
                }
                else
                {
                    if (typeName == "void")
                        publicType = typeRegistry.ImportType(typeof(IntPtr));

                    marshalType = typeRegistry.ImportType(typeof(IntPtr));
                }

                switch (typeName)
                {
                    case "char":
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(IntPtr));
                        break;
                    case "wchar_t":
                        publicType = typeRegistry.ImportType(typeof(string));
                        marshalType = typeRegistry.ImportType(typeof(IntPtr));
                        interopType.IsWideChar = true;
                        break;
                }
            }
            else
            {
                if (isTypeUsedInStruct)
                {
                    // Special case for Size type, as it is default marshal to IntPtr for method parameter
                    if (publicType.QualifiedName == globalNamespace.GetTypeName("PointerSize"))
                        marshalType = null;
                }
            }

            interopType.PublicType = publicType;
            interopType.HasMarshalType = (marshalType != null || marshallable.IsArray);
            if (marshalType == null)
                marshalType = publicType;
            interopType.MarshalType = marshalType;

            // Update the SizeOf according to the SizeOf MarshalType
            interopType.SizeOf = interopType.MarshalType.SizeOf * ((interopType.ArrayDimensionValue > 1) ? interopType.ArrayDimensionValue : 1);

            return interopType;
        }


    }
}
