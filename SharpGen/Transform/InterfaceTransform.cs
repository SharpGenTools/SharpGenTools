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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Transform
{
    /// <summary>
    /// Transforms a C++ interface to a C# interface.
    /// </summary>
    public class InterfaceTransform : TransformBase<CsInterface, CppInterface>, ITransformPreparer<CppInterface, CsInterface>, ITransformer<CsInterface>
    {
        private readonly Dictionary<Regex, InnerInterfaceMethod> _mapMoveMethodToInnerInterface = new();
        private TypeRegistry TypeRegistry => Ioc.TypeRegistry;
        private readonly NamespaceRegistry namespaceRegistry;
        private readonly IInteropSignatureTransform interopSignatureTransform;
        private readonly PropertyBuilder propertyBuilder;
        private readonly MethodOverloadBuilder methodOverloadBuilder;

        private readonly CsInterface DefaultCallbackable;
        private readonly CsInterface CppObjectType;

        public InterfaceTransform(NamingRulesManager namingRules,
                                  ITransformPreparer<CppMethod, CsMethod> methodPreparer,
                                  ITransformer<CsMethod> methodTransformer,
                                  NamespaceRegistry namespaceRegistry,
                                  IInteropSignatureTransform interopSignatureTransform,
                                  Ioc ioc) : base(namingRules, ioc)
        {
            MethodPreparer = methodPreparer;
            MethodTransformer = methodTransformer;
            this.namespaceRegistry = namespaceRegistry;
            this.interopSignatureTransform = interopSignatureTransform;
            propertyBuilder = new PropertyBuilder(Ioc);
            methodOverloadBuilder = new MethodOverloadBuilder(Ioc);

            var globalNamespace = Ioc.GlobalNamespace;

            CppObjectType = new CsInterface(null, globalNamespace.GetTypeName(WellKnownName.CppObject));
            DefaultCallbackable = new CsInterface(null, globalNamespace.GetTypeName(WellKnownName.ICallbackable))
            {
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
            CsInterface cSharpInterface = new(cppInterface, NamingRules.Rename(cppInterface))
            {
                IsFullyMapped = false
            };

            var nameSpace = namespaceRegistry.ResolveNamespace(cppInterface);
            nameSpace.Add(cSharpInterface);

            TypeRegistry.BindType(cppInterface.Name, cSharpInterface, source: cppInterface.ParentInclude?.Name);

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
            
            var baseType = TypeRegistry.FindBoundType(cppInterface.Base);
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

            // Handle Methods
            var methods = interfaceType.MethodList;
            List<CsMethod> specialOverloads = new();
            foreach (var cSharpMethod in methods)
            {
                MethodTransformer.Process(cSharpMethod);

                // Add specialized method overloads
                specialOverloads.AddRange(GenerateSpecialOverloads(cSharpMethod));
            }

            foreach (var overload in specialOverloads)
            {
                interfaceType.Add(overload);
                MethodTransform.CreateNativeInteropSignatures(interopSignatureTransform, overload);
            }

            MoveMethodsToInnerInterfaces(interfaceType);

            // If interfaceType is DualCallback, then need to generate a default implementation
            if (interfaceType.IsDualCallback)
            {
                var nativeCallback = CreateNativeCallbackType(interfaceType);
                interfaceType.Parent.Add(nativeCallback);
                CreateProperties(nativeCallback.Methods);
            }
            else if (!interfaceType.IsCallback && interfaceType.Base is {IsDualCallback: true})
            {
                interfaceType.Base = interfaceType.Base.GetNativeImplementationOrThis();
            }

            // N.B. Implicit dependency on CreateNativeCallbackType having already executed.
            //      Otherwise Native type gets ICallbackable instead of CppObject as a base.
            if (interfaceType.IsCallback)
                interfaceType.Base ??= DefaultCallbackable;

            CreateProperties(methods);
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
                    if (!keyValuePair.Key.Match(cppName).Success)
                        continue;

                    var innerInterfaceName = keyValuePair.Value.InnerInterface;
                    var parentInterfaceName = keyValuePair.Value.InheritedInterfaceName;

                    CsInterface parentCsInterface = null;

                    if (parentInterfaceName != null)
                    {
                        if (!mapInnerInterface.TryGetValue(parentInterfaceName, out parentCsInterface))
                        {
                            parentCsInterface = new CsInterface(null, parentInterfaceName);
                            mapInnerInterface.Add(parentInterfaceName, parentCsInterface);
                        }
                    }

                    if (!mapInnerInterface.TryGetValue(innerInterfaceName, out CsInterface innerCsInterface))
                    {
                        // TODO custom cppInterface?
                        innerCsInterface = new CsInterface((CppInterface)interfaceType.CppElement, innerInterfaceName)
                        {
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

        private CsInterface CreateNativeCallbackType(CsInterface interfaceType)
        {
            var cppInterface = (CppInterface) interfaceType.CppElement;
            var tagForInterface = cppInterface.Rule;

            var nativeCallbackBase = interfaceType.Base;

            var interfaceTypeName = tagForInterface.NativeCallbackName switch
            {
                { } name => name,
                _ => interfaceType.Name + "Native"
            };

            CsInterface nativeCallback = new(cppInterface, interfaceTypeName)
            {
                IsCallback = false,
                IsDualCallback = true,
                IBase = interfaceType,
                Base = nativeCallbackBase switch
                {
                    // If Parent is a DualInterface, then inherit from Default Callback
                    {IsDualCallback: true} baseInterface => baseInterface.GetNativeImplementationOrThis(),
                    _ => nativeCallbackBase ?? CppObjectType
                }
            };

            // Update nativeCallback from tag
            if (tagForInterface.NativeCallbackVisibility is {} visibility)
                nativeCallback.Visibility = visibility;

            interfaceType.NativeImplementation = nativeCallback;

            foreach (var method in interfaceType.MethodList)
            {
                var newCsMethod = (CsMethod) method.Clone();

                newCsMethod.Hidden = false;

                MethodTransform.CreateNativeInteropSignatures(interopSignatureTransform, newCsMethod);

                var keepImplementPublic = interfaceType.AutoGenerateShadow ||
                                          method.IsPublicVisibilityForced(interfaceType);

                if (!keepImplementPublic)
                {
                    newCsMethod.Visibility = Visibility.Internal;
                    newCsMethod.SuffixName("_");
                }

                nativeCallback.Add(newCsMethod);
            }

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

        private IEnumerable<CsMethod> GenerateSpecialOverloads(CsMethod csMethod)
        {
            var hasInterfaceArrayLike = csMethod.PublicParameters.Any(param => param.IsInInterfaceArrayLike);

            if (hasInterfaceArrayLike)
            {
                yield return methodOverloadBuilder.CreateInterfaceArrayOverload(csMethod);
            }

            if (hasInterfaceArrayLike || csMethod.RequestRawPtr)
            {
                yield return methodOverloadBuilder.CreateRawPtrOverload(csMethod);
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