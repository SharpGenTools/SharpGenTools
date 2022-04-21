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
using System.Diagnostics;
using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Transform;

/// <summary>
/// Transform a C++ method/function to a C# method.
/// </summary>
public class MethodTransform : TransformBase<CsMethod, CppMethod>, ITransformer<CsMethod>, ITransformPreparer<CppMethod, CsMethod>, ITransformer<CsFunction>, ITransformPreparer<CppFunction, CsFunction>
{
    private readonly GroupRegistry groupRegistry;
    private readonly MarshalledElementFactory factory;
    private readonly IInteropSignatureTransform signatureTransform;

    public MethodTransform(NamingRulesManager namingRules,
                           GroupRegistry groupRegistry,
                           MarshalledElementFactory factory,
                           IInteropSignatureTransform interopSignatureTransform,
                           Ioc ioc) : base(namingRules, ioc)
    {
        this.groupRegistry = groupRegistry;
        this.factory = factory;
        signatureTransform = interopSignatureTransform;
    }

    /// <summary>
    /// Prepares the specified C++ element to a C# element.
    /// </summary>
    /// <param name="cppMethod">The C++ element.</param>
    /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
    public override CsMethod Prepare(CppMethod cppMethod) => new(Ioc, cppMethod, NamingRules.Rename(cppMethod));

    public CsFunction Prepare(CppFunction cppFunction)
    {
        CsFunction function = new(Ioc, cppFunction, NamingRules.Rename(cppFunction));

        var groupName = cppFunction.Rule.Group;

        if (groupName == null)
        {
            Logger.Error(LoggingCodes.FunctionNotAttachedToGroup, "CppFunction [{0}] is not tagged and attached to any Class/FunctionGroup", cppFunction);
            return null;
        }

        var csClass = groupRegistry.FindGroup(groupName);

        if (csClass == null)
        {
            Logger.Error(LoggingCodes.FunctionNotAttachedToGroup, "CppFunction [{0}] is not attached to a Class/FunctionGroup", cppFunction);
            return null;
        }

        // Add the function to the ClassType
        csClass.Add(function);

        return function;
    }

    /// <summary>
    /// Processes the specified C# element to complete the mapping process between the C++ and C# element.
    /// </summary>
    /// <param name="csElement">The C# element.</param>
    public override void Process(CsMethod csElement)
    {
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

        // For methods, the tag "type" is only used for return type
        // So we are overriding the return type here
        var methodRule = cppMethod.Rule;
        if (methodRule.MappingType != null)
            cppMethod.ReturnValue.Rule.MappingType = methodRule.MappingType;

        // Get the inferred return type
        method.ReturnValue = factory.Create(cppMethod.ReturnValue);

        var parameters = cppMethod.Parameters.ToArray();
        var parameterNames = NamingRules.Rename(parameters);

        Debug.Assert(parameters.Length == parameterNames.Count);

        uint? outCount = 0;

        // Iterates on parameters to convert them to C# parameters
        for (var index = 0; index < parameters.Length; index++)
        {
            var cppParameter = parameters[index];
            var parameterName = parameterNames[index];
            var csParameter = factory.Create(cppParameter, parameterName);
            method.Add(csParameter);

            if (!outCount.HasValue)
                continue;

            if (csParameter.IsOut && csParameter.Relations.Count == 0)
                ++outCount;

            if (cppParameter.Rule.ParameterUsedAsReturnType == false)
                outCount = null;
        }

        if (outCount == 1)
            TransformOutParametersToReturnValues(method);
    }

    private void TransformOutParametersToReturnValues(CsCallable method)
    {
        if (method.HasReturnTypeValue)
            return;

        var outCsParameter = method.Parameters.Single(x => x.IsOut && x.Relations.Count == 0);

        if (outCsParameter.IsArray)
            return;

        var outCppParameter = outCsParameter.CppElement;

        if (outCppParameter is not CppParameter {Attribute: var attribute})
            return;

        if ((attribute & ParamAttribute.Buffer) == ParamAttribute.Buffer)
            return;

        if ((attribute & ParamAttribute.Fast) == ParamAttribute.Fast)
            return;

        if ((attribute & ParamAttribute.Value) == ParamAttribute.Value)
            return;

        outCsParameter.MarkUsedAsReturn();

        if (outCppParameter.Rule.ParameterUsedAsReturnType == true)
            Logger.Message("Parameter [{0}] has redundant return rule specification", outCppParameter.FullName);
    }

    internal static void CreateNativeInteropSignatures(IInteropSignatureTransform sigTransform, CsCallable callable)
    {
        callable.InteropSignatures = new Dictionary<PlatformDetectionType, InteropMethodSignature>();
        foreach (var sig in sigTransform.GetInteropSignatures(callable))
            callable.InteropSignatures.Add(sig.Key, sig.Value);
    }
}