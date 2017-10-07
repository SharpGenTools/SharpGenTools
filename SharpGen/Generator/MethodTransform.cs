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

namespace SharpGen.Generator
{
    /// <summary>
    /// Transform a C++ method/function to a C# method.
    /// </summary>
    public class MethodTransform : TransformBase<CsMethod, CppMethod>, ITransform<CsFunction, CppFunction>
    {

        public SyntaxNode GenerateCode(CsFunction csElement)
        {
            return GenerateCode((CsMethod)csElement);
        }

        public override SyntaxNode GenerateCode(CsMethod csElement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares the specified C++ element to a C# element.
        /// </summary>
        /// <param name="cppElement">The C++ element.</param>
        /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
        public override CsMethod Prepare(CppMethod cppMethod) => new CsMethod(cppMethod);

        public CsFunction Prepare(CppFunction cppFunction)
        {
            var cSharpFunction = new CsFunction(cppFunction);
            // All functions must have a tag
            var tag = cppFunction.GetTagOrDefault<MappingRule>();

            if (tag == null || tag.CsClass == null)
            {
                Logger.Error("CppFunction [{0}] is not tagged and attached to any Class/FunctionGroup", cppFunction);
                return null;
            }

            var csClass = Manager.FindCsClassContainer(tag.CsClass);

            if (csClass == null)
            {
                Logger.Error("CppFunction [{0}] is not attached to a Class/FunctionGroup", cppFunction);
                return null;
            }

            // Set the DllName for this function
            cSharpFunction.DllName = tag.FunctionDllName;

            // Add the function to the ClassType
            csClass.Add(cSharpFunction);

            // Map the C++ name to the CSharpType
            Manager.BindType(cppFunction.Name, cSharpFunction);

            return cSharpFunction;
        }

        /// <summary>
        /// Processes the specified C# element to complete the mapping process between the C++ and C# element.
        /// </summary>
        /// <param name="csElement">The C# element.</param>
        public override void Process(CsMethod csElement)
        {
            try
            {
                var csMethod = csElement;
                Logger.PushContext("Method {0}", csMethod.CppElement);

                ProcessMethod(csMethod);

                RegisterNativeInteropSignature(csMethod);
            }
            finally
            {
                Logger.PopContext();
            }
        }

        public void Process(CsFunction csFunction)
        {
            csFunction.Visibility = csFunction.Visibility | Visibility.Static;
            Process((CsMethod)csFunction);
        }

        /// <summary>
        /// Processes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        private void ProcessMethod(CsMethod method)
        {
            var cppMethod = (CppMethod)method.CppElement;

            method.Name = NamingRules.Rename(cppMethod);
            method.Offset = cppMethod.Offset;

            // For methods, the tag "type" is only used for return type
            // So we are overriding the return type here
            var tag = cppMethod.GetTagOrDefault<MappingRule>();
            if (tag.MappingType != null)
                cppMethod.ReturnType.Tag = new MappingRule { MappingType = tag.MappingType };

            // Apply any offset to the method's vtable
            method.Offset += tag.LayoutOffsetTranslate;

            // Get the inferred return type
            method.ReturnType = Manager.GetCsType<CsMarshalBase>(cppMethod.ReturnType);

            // Hide return type only if it is a HRESULT and AlwaysReturnHResult is false
            if (method.CheckReturnType && method.ReturnType.PublicType != null &&
                method.ReturnType.PublicType.QualifiedName == Manager.GlobalNamespace.GetTypeName("Result"))
            {
                method.HideReturnType = !method.AlwaysReturnHResult;
            }

            // Iterates on parameters to convert them to C# parameters
            foreach (var cppParameter in cppMethod.Parameters)
            {
                var cppAttribute = cppParameter.Attribute;
                var paramTag = cppParameter.GetTagOrDefault<MappingRule>();

                bool hasArray = cppParameter.IsArray || ((cppAttribute & ParamAttribute.Buffer) != 0);
                bool hasParams = (cppAttribute & ParamAttribute.Params) == ParamAttribute.Params;
                bool isOptional = (cppAttribute & ParamAttribute.Optional) != 0;

                var paramMethod = Manager.GetCsType<CsParameter>(cppParameter);

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
                    marshalType = Manager.ImportType(typeof(IntPtr));

                    // --------------------------------------------------------------------------------
                    // Handling Parameter Interface
                    // --------------------------------------------------------------------------------
                    if (publicType is CsInterface)
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
                            CsInterface publicCsInterface = (CsInterface)publicType;
                            if (publicCsInterface.IsCallback)
                            {
                                publicType = Manager.ImportType(typeof(IntPtr));
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
                        //else if ((cppParameter.Attribute & ParamAttribute.InOut) != 0)
                        //    parameterAttribute = method.ParameterAttribute.Ref;
                        else if ((cppAttribute & ParamAttribute.Out) != 0)
                            parameterAttribute = CsParameterAttribute.Out;
                    }
                    else
                    {
                        // If a pointer to array of bool are handle as array of int
                        if (paramMethod.IsBoolToInt && (cppAttribute & ParamAttribute.Buffer) != 0)
                            publicType = Manager.ImportType(typeof(int));

                        // --------------------------------------------------------------------------------
                        // Handling Parameter Interface
                        // --------------------------------------------------------------------------------


                        if ((cppAttribute & ParamAttribute.In) != 0)
                        {
                            parameterAttribute = publicType.Type == typeof(IntPtr) || publicType.Name == Manager.GlobalNamespace.GetTypeName("FunctionCallback") ||
                                                 publicType.Type == typeof(string)
                                                     ? CsParameterAttribute.In
                                                     : CsParameterAttribute.RefIn;
                        }
                        else if ((cppAttribute & ParamAttribute.InOut) != 0)
                        {
                            if ((cppAttribute & ParamAttribute.Optional) != 0)
                            {
                                publicType = Manager.ImportType(typeof(IntPtr));
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
                        if (cppParameter.TypeName == "void" && (cppAttribute & ParamAttribute.Buffer) != 0)
                        {
                            hasArray = false;
                            parameterAttribute = CsParameterAttribute.In;
                        }
                        else if (publicType.Type == typeof(string) && (cppAttribute & ParamAttribute.Out) != 0)
                        {
                            publicType = Manager.ImportType(typeof(IntPtr));
                            parameterAttribute = CsParameterAttribute.In;
                            hasArray = false;
                        }
                        else if (publicType is CsStruct &&
                                 (parameterAttribute == CsParameterAttribute.Out || hasArray || parameterAttribute == CsParameterAttribute.RefIn || parameterAttribute == CsParameterAttribute.Ref))
                        {
                            // Set IsOut on structure to generate proper marshalling
                            (publicType as CsStruct).IsOut = true;
                        }
                    }
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
        private void RegisterNativeInteropSignature(CsMethod csMethod)
        {
            // Tag if the method is a function
            var cSharpInteropCalliSignature = new InteropMethodSignature { IsFunction = (csMethod is CsFunction) };

            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (csMethod.ReturnType.PublicType is CsStruct || csMethod.ReturnType.PublicType is CsEnum)
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
                    var returnQualifiedName = csMethod.ReturnType.PublicType.QualifiedName;
                    if (returnQualifiedName == Manager.GlobalNamespace.GetTypeName("Result"))
                        cSharpInteropCalliSignature.ReturnType = typeof(int);
                    else if (returnQualifiedName == Manager.GlobalNamespace.GetTypeName("PointerSize"))
                        cSharpInteropCalliSignature.ReturnType = typeof(void*);
                    else
                        cSharpInteropCalliSignature.ReturnType = csMethod.ReturnType.PublicType.QualifiedName;
                }
            }
            else if (csMethod.ReturnType.MarshalType.Type != null)
            {
                Type type = csMethod.ReturnType.MarshalType.Type;
                cSharpInteropCalliSignature.ReturnType = type;
            }
            else
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid return type {0} for method {1}", csMethod.ReturnType.PublicType.QualifiedName, csMethod.CppElement));
            }

            // Handle Parameters
            foreach (var param in csMethod.Parameters)
            {
                InteropType interopType;
                string publicName = param.PublicType.QualifiedName;
                // Patch for Mono bug with structs marshalling and calli.
                if (publicName == Manager.GlobalNamespace.GetTypeName("PointerSize"))
                {
                    interopType = typeof(void*);
                }
                else if (param.MarshalType.Type == null)
                {
                    if (param.PublicType is CsStruct)
                    {
                        // If parameter is a struct, then a LocalInterop is needed
                        interopType = param.PublicType.QualifiedName;
                        cSharpInteropCalliSignature.IsLocal = true;
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid parameter {0} for method {1}", param.PublicType.QualifiedName, csMethod.CppElement));
                    }
                }
                else
                {
                    Type type = param.MarshalType.Type;
                    // Patch for Mono bug with structs marshalling and calli.
                    if (type == typeof(IntPtr))
                        type = typeof(void*);
                    interopType = type;
                }

                cSharpInteropCalliSignature.ParameterTypes.Add(interopType);
            }

            var assembly = csMethod.GetParent<CsAssembly>();
            cSharpInteropCalliSignature = assembly.Interop.Add(cSharpInteropCalliSignature);

            csMethod.Interop = cSharpInteropCalliSignature;
        }
    }
}