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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Transform
{
    /// <summary>
    /// Transforms a C++ interface to a C# interface.
    /// </summary>
    public class InterfaceTransform : TransformBase<CsInterface, CppInterface>, ITransformPreparer<CppInterface, CsInterface>, ITransformer<CsInterface>
    {
        private readonly Dictionary<Regex, InnerInterfaceMethod> _mapMoveMethodToInnerInterface = new Dictionary<Regex, InnerInterfaceMethod>();
        private readonly TypeRegistry typeRegistry;
        private readonly NamespaceRegistry namespaceRegistry;
        private readonly PropertyBuilder propertyBuilder;
        private readonly MethodOverloadBuilder methodOverloadBuilder;

        private readonly CsInterface DefaultCallbackable;
        private readonly CsInterface CppObjectType;

        public InterfaceTransform(
            NamingRulesManager namingRules,
            Logger logger,
            GlobalNamespaceProvider globalNamespace,
            ITransformPreparer<CppMethod, CsMethod> methodPreparer,
            ITransformer<CsMethod> methodTransformer,
            TypeRegistry typeRegistry,
            NamespaceRegistry namespaceRegistry)
            : base(namingRules, logger)
        {
            MethodPreparer = methodPreparer;
            MethodTransformer = methodTransformer;
            this.typeRegistry = typeRegistry;
            this.namespaceRegistry = namespaceRegistry;
            propertyBuilder = new PropertyBuilder(globalNamespace);
            methodOverloadBuilder = new MethodOverloadBuilder(globalNamespace, typeRegistry);

            CppObjectType = new CsInterface { Name = globalNamespace.GetTypeName(WellKnownName.CppObject) };
            DefaultCallbackable = new CsInterface
            {
                Name = globalNamespace.GetTypeName(WellKnownName.ICallbackable),
                ShadowName = globalNamespace.GetTypeName(WellKnownName.CppObjectShadow),
                VtblName = globalNamespace.GetTypeName(WellKnownName.CppObjectVtbl)
            };
        }

        /// <summary>
        /// Gets the method transformer.
        /// </summary>
        /// <value>The method transformer.</value>
        private ITransformPreparer<CppMethod, CsMethod> MethodPreparer { get; }

        private ITransformer<CsMethod> MethodTransformer { get; }

        /// <summary>
        /// Moves the methods to an inner C# interface.
        /// </summary>
        /// <param name="methodNameRegExp">The method name regexp query.</param>
        /// <param name="innerInterface">The C# inner interface.</param>
        /// <param name="propertyNameAccess">The name of the property to access the inner interface.</param>
        /// <param name="inheritedInterfaceName">Name of the inherited interface.</param>
        public void MoveMethodsToInnerInterface(string methodNameRegExp, string innerInterface, string propertyNameAccess,
                                                 string inheritedInterfaceName = null)
        {
            _mapMoveMethodToInnerInterface.Add(new Regex("^" + methodNameRegExp + "$"),
                                               new InnerInterfaceMethod(innerInterface, propertyNameAccess,
                                                                        inheritedInterfaceName));
        }

        /// <summary>
        /// Prepares the specified C++ element to a C# element.
        /// </summary>
        /// <param name="cppInterface">The C++ element.</param>
        /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
        public override CsInterface Prepare(CppInterface cppInterface)
        {
            // IsFullyMapped to false => The structure is being mapped
            var cSharpInterface = new CsInterface(cppInterface) { IsFullyMapped = false };
            var nameSpace = namespaceRegistry.ResolveNamespace(cppInterface);
            cSharpInterface.Name = NamingRules.Rename(cppInterface);
            nameSpace.Add(cSharpInterface);

            typeRegistry.BindType(cppInterface.Name, cSharpInterface);

            foreach (var cppMethod in cppInterface.Methods)
            {
                cSharpInterface.Add(MethodPreparer.Prepare(cppMethod));
            }

            return cSharpInterface;
        }

        /// <summary>
        /// Processes the specified interface type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        public override void Process(CsInterface interfaceType)
        {
            if (interfaceType.IsFullyMapped)
                return;

            // Set IsFullyMapped to avoid recursive mapping
            interfaceType.IsFullyMapped = true;

            var cppInterface = (CppInterface)interfaceType.CppElement;
            
            var baseType = typeRegistry.FindBoundType(cppInterface.Base);
            if (baseType != null)
            {
                interfaceType.Base = (CsInterface)baseType;

                // Process base if it's not mapped already
                if (!interfaceType.Base.IsFullyMapped)
                    Process(interfaceType.Base);
            }
            else
            {
                if (!interfaceType.IsCallback)
                    interfaceType.Base = CppObjectType;
            }

            // If Guid is null we try to recover it from a declared GUID
            FindGuidForInterface(interfaceType);

            // Handle Methods
            var generatedMethods = interfaceType.Methods.ToList();
            foreach (var cSharpMethod in generatedMethods)
            {
                MethodTransformer.Process(cSharpMethod);

                // Add specialized method overloads
                GenerateSpecialOverloads(interfaceType, cSharpMethod);
            }

            MoveMethodsToInnerInterfaces(interfaceType);

            // If interfaceType is DualCallback, then need to generate a default implementation
            if (interfaceType.IsDualCallback)
            {
                var nativeCallback = CreateNativeCallbackType(interfaceType);
                interfaceType.Parent.Add(nativeCallback);
                CreateProperties(nativeCallback.Methods);
            }
            else
            {
                if (!interfaceType.IsCallback && interfaceType.Base != null && interfaceType.Base.IsDualCallback)
                {
                    interfaceType.Base = interfaceType.Base.GetNativeImplementationOrThis();
                }
            }

            CreateProperties(generatedMethods);


            if (interfaceType.IsCallback)
            {
                if (interfaceType.Base == null)
                    interfaceType.Base = DefaultCallbackable;
            }
        }

        private static void FindGuidForInterface(CsInterface interfaceType)
        {
            var cppInterface = (CppInterface)interfaceType.CppElement;
            if (string.IsNullOrEmpty(cppInterface.Guid))
            {
                var finder = new CppElementFinder(cppInterface.ParentInclude);

                var cppGuid = finder.Find<CppGuid>("IID_" + cppInterface.Name).FirstOrDefault();
                if (cppGuid != null)
                {
                    interfaceType.Guid = cppGuid.Guid.ToString();
                }
            }
        }

        private void MoveMethodsToInnerInterfaces(CsInterface interfaceType)
        {
            // Dispatch method to inner interface if any
            var mapInnerInterface = new Dictionary<string, CsInterface>();

            // Make a copy of the methods
            var methods = interfaceType.Methods.ToList();
            foreach (var csMethod in methods)
            {
                var cppName = interfaceType.CppElementName + "::" + csMethod.CppElement.Name;
                foreach (var keyValuePair in _mapMoveMethodToInnerInterface)
                {
                    if (keyValuePair.Key.Match(cppName).Success)
                    {
                        var innerInterfaceName = keyValuePair.Value.InnerInterface;
                        var parentInterfaceName = keyValuePair.Value.InheritedInterfaceName;

                        CsInterface parentCsInterface = null;

                        if (parentInterfaceName != null)
                        {
                            if (!mapInnerInterface.TryGetValue(parentInterfaceName, out parentCsInterface))
                            {
                                parentCsInterface = new CsInterface(null) { Name = parentInterfaceName };
                                mapInnerInterface.Add(parentInterfaceName, parentCsInterface);
                            }
                        }

                        if (!mapInnerInterface.TryGetValue(innerInterfaceName, out CsInterface innerCsInterface))
                        {
                            // TODO custom cppInterface?
                            innerCsInterface = new CsInterface((CppInterface)interfaceType.CppElement)
                            {
                                Name = innerInterfaceName,
                                PropertyAccessName = keyValuePair.Value.PropertyAccessName,
                                Base = parentCsInterface ?? CppObjectType
                            };

                            // Add inner interface to root interface
                            interfaceType.Add(innerCsInterface);
                            interfaceType.Parent.Add(innerCsInterface);

                            // Move method to inner interface
                            mapInnerInterface.Add(innerInterfaceName, innerCsInterface);
                        }

                        interfaceType.Remove(csMethod);
                        innerCsInterface.Add(csMethod);
                        break;
                    }
                }
            }
        }

        private CsInterface CreateNativeCallbackType(CsInterface interfaceType)
        {
            var cppInterface = (CppInterface)interfaceType.CppElement;
            var tagForInterface = cppInterface.GetMappingRule();
            var nativeCallback = new CsInterface(interfaceType.CppElement as CppInterface)
            {
                Name = interfaceType.Name + "Native"
            };

            // Update nativeCallback from tag
            if (tagForInterface != null)
            {
                if (tagForInterface.NativeCallbackVisibility.HasValue)
                    nativeCallback.Visibility = tagForInterface.NativeCallbackVisibility.Value;
                if (tagForInterface.NativeCallbackName != null)
                    nativeCallback.Name = tagForInterface.NativeCallbackName;
            }

            nativeCallback.Base = interfaceType.Base ?? CppObjectType;

            // If Parent is a DualInterface, then inherit from Default Callback
            if (interfaceType.Base is CsInterface baseInterface && baseInterface.IsDualCallback)
            {
                nativeCallback.Base = baseInterface.GetNativeImplementationOrThis();
            }

            nativeCallback.IBase = interfaceType;
            interfaceType.NativeImplementation = nativeCallback;

            foreach (var innerElement in interfaceType.Items)
            {
                if (innerElement is CsMethod method)
                {
                    var newCsMethod = (CsMethod)method.Clone();
                    var tagForMethod = method.CppElement.GetMappingRule();
                    var keepMethodPublic = interfaceType.AutoGenerateShadow || tagForMethod.IsKeepImplementPublic == true;
                    if (!keepMethodPublic)
                    {
                        newCsMethod.Visibility = Visibility.Internal;
                        newCsMethod.Name = newCsMethod.Name + "_";
                    }
                    nativeCallback.Add(newCsMethod);
                }
            }
            
            nativeCallback.IsCallback = false;
            nativeCallback.IsDualCallback = true;
            return nativeCallback;
        }

        /// <summary>
        /// Creates C# properties for the given set of methods.
        /// </summary>
        /// <param name="methods">The methods.</param>
        private void CreateProperties(IEnumerable<CsMethod> methods)
        {
            var cSharpProperties = propertyBuilder.CreateProperties(methods);

            // Add the property to the parentContainer
            foreach (var property in cSharpProperties.Values)
            {
                propertyBuilder.AttachPropertyToParent(property);
            }
        }

        private void GenerateSpecialOverloads(CsInterface interfaceType, CsMethod csMethod)
        {
            var hasInterfaceArrayLike = csMethod.Parameters.Any(param => param.IsInInterfaceArrayLike);

            if (hasInterfaceArrayLike)
            {
                interfaceType.Add(methodOverloadBuilder.CreateInterfaceArrayOverload(csMethod));
            }

            if (hasInterfaceArrayLike || csMethod.RequestRawPtr)
            {
                interfaceType.Add(methodOverloadBuilder.CreateRawPtrOverload(csMethod));
            }
        }

        /// <summary>
        /// Private class used for inner interface method creation.
        /// </summary>
        private class InnerInterfaceMethod
        {
            public readonly string InnerInterface;
            public readonly string PropertyAccessName;
            public readonly string InheritedInterfaceName;

            public InnerInterfaceMethod(string innerInterface, string propertyAccess, string inheritedInterfaceName)
            {
                InnerInterface = innerInterface;
                PropertyAccessName = propertyAccess;
                InheritedInterfaceName = inheritedInterfaceName;
            }
        }
    }
}