// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace SharpGen.Transform
{
    /// <summary>
    /// Transform a C++ method/function to a C# method.
    /// </summary>
    public class MethodTransform : TransformBase<CsMethod, CppMethod>, ITransformer<CsMethod>, ITransformPreparer<CppMethod, CsMethod>, ITransformer<CsFunction>, ITransformPreparer<CppFunction, CsFunction>
    {
        private readonly GroupRegistry groupRegistry;
        private readonly MarshalledElementFactory factory;
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly TypeRegistry typeRegistry;

        public MethodTransform(
            NamingRulesManager namingRules,
            Logger logger,
            GroupRegistry groupRegistry,
            MarshalledElementFactory factory,
            GlobalNamespaceProvider globalNamespace,
            TypeRegistry typeRegistry)
            : base(namingRules, logger)
        {
            this.groupRegistry = groupRegistry;
            this.factory = factory;
            this.globalNamespace = globalNamespace;
            this.typeRegistry = typeRegistry;
        }

        /// <summary>
        /// Prepares the specified C++ element to a C# element.
        /// </summary>
        /// <param name="cppMethod">The C++ element.</param>
        /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
        public override CsMethod Prepare(CppMethod cppMethod) => new CsMethod(cppMethod);

        public CsFunction Prepare(CppFunction cppFunction)
        {
            var cSharpFunction = new CsFunction(cppFunction);
            // All functions must have a tag
            var tag = cppFunction.GetMappingRule();

            if (tag == null || tag.Group == null)
            {
                Logger.Error("CppFunction [{0}] is not tagged and attached to any Class/FunctionGroup", cppFunction);
                return null;
            }

            var csClass = groupRegistry.FindGroup(tag.Group);

            if (csClass == null)
            {
                Logger.Error("CppFunction [{0}] is not attached to a Class/FunctionGroup", cppFunction);
                return null;
            }

            // Set the DllName for this function
            cSharpFunction.DllName = tag.FunctionDllName;

            // Add the function to the ClassType
            csClass.Add(cSharpFunction);

            return cSharpFunction;
        }

        /// <summary>
        /// Processes the specified C# element to complete the mapping process between the C++ and C# element.
        /// </summary>
        /// <param name="csElement">The C# element.</param>
        public override void Process(CsMethod csElement)
        {
            var cppMethod = (CppMethod)csElement.CppElement;

            csElement.Offset = cppMethod.Offset;

            var methodRule = cppMethod.GetMappingRule();

            // Apply any offset to the method's vtable
            csElement.Offset += methodRule.LayoutOffsetTranslate;

            ProcessCallable(csElement, false);
        }

        private void ProcessCallable(CsCallable csElement, bool isFunction)
        {
            try
            {
                var csMethod = csElement;
                Logger.PushContext("Method {0}", csMethod.CppElement);

                ProcessMethod(csMethod);

                RegisterNativeInteropSignature(csMethod, isFunction);
            }
            finally
            {
                Logger.PopContext();
            }
        }

        public void Process(CsFunction csFunction)
        {
            csFunction.Visibility = csFunction.Visibility | Visibility.Static;
            ProcessCallable(csFunction, true);
        }

        /// <summary>
        /// Processes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        private void ProcessMethod(CsCallable method)
        {
            var cppMethod = (CppCallable)method.CppElement;

            method.Name = NamingRules.Rename(cppMethod);

            // For methods, the tag "type" is only used for return type
            // So we are overriding the return type here
            var methodRule = cppMethod.GetMappingRule();
            if (methodRule.MappingType != null)
                cppMethod.ReturnValue.Rule = new MappingRule { MappingType = methodRule.MappingType };

            // Get the inferred return type
            method.ReturnValue = factory.Create<CsReturnValue>(cppMethod.ReturnValue);

            // Hide return type only if it is a HRESULT and AlwaysReturnHResult is false
            if (method.CheckReturnType && method.ReturnValue.PublicType != null &&
                method.ReturnValue.PublicType.QualifiedName == globalNamespace.GetTypeName("Result"))
            {
                method.HideReturnType = !method.AlwaysReturnHResult;
            }

            // Iterates on parameters to convert them to C# parameters
            foreach (var cppParameter in cppMethod.Parameters)
            {
                var cppAttribute = cppParameter.Attribute;
                var paramRule = cppParameter.GetMappingRule();

                bool hasArray = cppParameter.IsArray || ((cppAttribute & ParamAttribute.Buffer) != 0);
                bool hasParams = (cppAttribute & ParamAttribute.Params) == ParamAttribute.Params;
                bool isOptional = (cppAttribute & ParamAttribute.Optional) != 0;

                var paramMethod = factory.Create<CsParameter>(cppParameter);

                paramMethod.Name = NamingRules.Rename(cppParameter);

                bool hasPointer = paramMethod.HasPointer;

                var publicType = paramMethod.PublicType;
                var marshalType = paramMethod.MarshalType;

                CsParameterAttribute parameterAttribute = CsParameterAttribute.In;

                if (hasArray)
                    hasPointer = true;

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

                            // If Interface is a callback, use IntPtr as a public marshalling type
                            if (publicInterface.IsCallback)
                            {
                                publicType = typeRegistry.ImportType(typeof(IntPtr));
                                // By default, set the Visibility to internal for methods using callbacks
                                // as we need to provide user method. Don't do this on functions as they
                                // are already hidden by the container
                                if (!(method is CsFunction))
                                {
                                    method.Visibility = Visibility.Internal;
                                    method.Name = method.Name + "_";
                                }
                            }
                        }
                        else if ((cppAttribute & ParamAttribute.Out) != 0)
                            parameterAttribute = CsParameterAttribute.Out;
                    }
                    else
                    {
                        // If a pointer to array of bool are handle as array of int
                        if (paramMethod.IsBoolToInt && (cppAttribute & ParamAttribute.Buffer) != 0)
                            publicType = typeRegistry.ImportType(typeof(int));

                        // --------------------------------------------------------------------------------
                        // Handling Parameter Interface
                        // --------------------------------------------------------------------------------


                        if ((cppAttribute & ParamAttribute.In) != 0)
                        {
                            var fundamentalType = (publicType as CsFundamentalType)?.Type;
                            parameterAttribute = fundamentalType == typeof(IntPtr)
                                                || publicType.Name == globalNamespace.GetTypeName("FunctionCallback")
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

                paramMethod.HasPointer = hasPointer;
                paramMethod.Attribute = parameterAttribute;
                paramMethod.IsArray = hasArray;
                paramMethod.HasParams = hasParams;
                paramMethod.HasPointer = hasPointer;
                paramMethod.PublicType = publicType ?? throw new ArgumentException("Public type cannot be null");
                paramMethod.MarshalType = marshalType;
                paramMethod.IsOptional = isOptional;

                // Force IsString to be only string (due to Buffer attribute)
                if (paramMethod.IsString)
                    paramMethod.IsArray = false;

                method.Add(paramMethod);
            }
        }

        /// <summary>
        /// Registers the native interop signature.
        /// </summary>
        /// <param name="csMethod">The cs method.</param>
        private void RegisterNativeInteropSignature(CsCallable csMethod, bool isFunction)
        {
            // Tag if the method is a function
            var cSharpInteropCalliSignature = new InteropMethodSignature { IsFunction = isFunction };

            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (csMethod.ReturnValue.PublicType is CsStruct || csMethod.ReturnValue.PublicType is CsEnum)
            {
                // Return type and 1st parameter are implicitly a pointer to the structure to fill 
                if (csMethod.IsReturnStructLarge)
                {
                    cSharpInteropCalliSignature.ReturnType = typeof(void*);
                    cSharpInteropCalliSignature.ParameterTypes.Add(typeof(void*));
                }
                else
                {
                    // Patch for Mono bug with structs marshalling and calli.
                    var returnQualifiedName = csMethod.ReturnValue.PublicType.QualifiedName;
                    if (returnQualifiedName == globalNamespace.GetTypeName("Result"))
                        cSharpInteropCalliSignature.ReturnType = typeof(int);
                    else if (returnQualifiedName == globalNamespace.GetTypeName("PointerSize"))
                        cSharpInteropCalliSignature.ReturnType = typeof(void*);
                    else
                        cSharpInteropCalliSignature.ReturnType = csMethod.ReturnValue.PublicType.QualifiedName;
                }
            }
            else if (csMethod.ReturnValue.MarshalType is CsFundamentalType fundamentalReturn)
            {
                cSharpInteropCalliSignature.ReturnType = fundamentalReturn.Type;
            }
            else
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid return type {0} for method {1}", csMethod.ReturnValue.PublicType.QualifiedName, csMethod.CppElement));
            }

            // Handle Parameters
            foreach (var param in csMethod.Parameters)
            {
                InteropType interopType;
                var publicName = param.PublicType.QualifiedName;
                // Patch for Mono bug with structs marshalling and calli.
                if (publicName == globalNamespace.GetTypeName("PointerSize"))
                {
                    interopType = typeof(void*);
                }
                else if (param.HasPointer)
                {
                    interopType = typeof(void*);
                }
                else if (param.MarshalType is CsFundamentalType marshalFundamental)
                {
                    var type = marshalFundamental.Type;
                    // Patch for Mono bug with structs marshalling and calli.
                    if (type == typeof(IntPtr))
                        type = typeof(void*);
                    interopType = type;
                }
                else if (param.PublicType is CsFundamentalType publicFundamental)
                {
                    var type = publicFundamental.Type;
                    // Patch for Mono bug with structs marshalling and calli.
                    if (type == typeof(IntPtr))
                        type = typeof(void*);
                    interopType = type;
                }
                else if (param.PublicType is CsStruct csStruct)
                {
                    // If parameter is a struct, then a LocalInterop is needed
                    if (csStruct.HasMarshalType)
                    {
                        interopType = $"{csStruct.QualifiedName}.__Native";
                    }
                    else
                    {
                        interopType = csStruct.QualifiedName; 
                    }
                    cSharpInteropCalliSignature.IsLocal = true;
                }
                else if (param.PublicType is CsEnum csEnum)
                {
                    interopType = csEnum.UnderlyingType.Type;
                }
                else
                {
                    throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid parameter {0} for method {1}", param.PublicType.QualifiedName, csMethod.CppElement));
                }

                cSharpInteropCalliSignature.ParameterTypes.Add(interopType);
            }

            var assembly = csMethod.GetParent<CsAssembly>();
            cSharpInteropCalliSignature = assembly.Interop.Add(cSharpInteropCalliSignature);

            csMethod.Interop = cSharpInteropCalliSignature;
        }
    }
}