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
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

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
        private readonly IInteropSignatureTransform signatureTransform;

        public MethodTransform(NamingRulesManager namingRules,
                               Logger logger,
                               GroupRegistry groupRegistry,
                               MarshalledElementFactory factory,
                               GlobalNamespaceProvider globalNamespace,
                               IInteropSignatureTransform interopSignatureTransform)
            : base(namingRules, logger)
        {
            this.groupRegistry = groupRegistry;
            this.factory = factory;
            this.globalNamespace = globalNamespace;
            signatureTransform = interopSignatureTransform;
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
                Logger.Error(LoggingCodes.FunctionNotAttachedToGroup, "CppFunction [{0}] is not tagged and attached to any Class/FunctionGroup", cppFunction);
                return null;
            }

            var csClass = groupRegistry.FindGroup(tag.Group);

            if (csClass == null)
            {
                Logger.Error(LoggingCodes.FunctionNotAttachedToGroup, "CppFunction [{0}] is not attached to a Class/FunctionGroup", cppFunction);
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
            csElement.WindowsOffset = cppMethod.WindowsOffset;

            var methodRule = cppMethod.GetMappingRule();

            // Apply any offset to the method's vtable
            csElement.Offset += methodRule.LayoutOffsetTranslate;
            csElement.WindowsOffset += methodRule.LayoutOffsetTranslate;

            ProcessCallable(csElement);
        }

        private void ProcessCallable(CsCallable csCallable)
        {
            try
            {
                Logger.PushContext("Method {0}", csCallable.CppElement);

                ProcessMethod(csCallable);

                CreateNativeInteropSignatures(signatureTransform, csCallable);
            }
            finally
            {
                Logger.PopContext();
            }
        }

        public void Process(CsFunction csFunction)
        {
            csFunction.Visibility |= Visibility.Static;
            ProcessCallable(csFunction);
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
            method.ReturnValue = factory.Create(cppMethod.ReturnValue);

            if (method.ReturnValue.PublicType is CsInterface {IsCallback: true} iface)
            {
                method.ReturnValue.PublicType = iface.GetNativeImplementationOrThis();
            }

            // Hide return type only if it is a HRESULT and AlwaysReturnHResult is false
            if (method.CheckReturnType && method.ReturnValue.PublicType != null &&
                method.ReturnValue.PublicType.QualifiedName == globalNamespace.GetTypeName(WellKnownName.Result))
            {
                method.HideReturnType = !method.AlwaysReturnHResult;
            }

            // Iterates on parameters to convert them to C# parameters
            foreach (var cppParameter in cppMethod.Parameters)
            {
                var paramMethod = factory.Create(cppParameter);

                paramMethod.Name = NamingRules.Rename(cppParameter);

                method.Add(paramMethod);
            }
        }

        internal static void CreateNativeInteropSignatures(IInteropSignatureTransform sigTransform, CsCallable callable)
        {
            callable.InteropSignatures = new Dictionary<PlatformDetectionType, InteropMethodSignature>();
            foreach (var sig in sigTransform.GetInteropSignatures(callable))
                callable.InteropSignatures.Add(sig.Key, sig.Value);
        }
    }
}