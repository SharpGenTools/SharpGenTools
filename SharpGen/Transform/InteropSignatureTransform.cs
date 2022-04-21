using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;

namespace SharpGen.Transform;

internal sealed class InteropSignatureTransform : IInteropSignatureTransform
{
    private GlobalNamespaceProvider Provider => ioc.GlobalNamespace;
    private Logger Logger => ioc.Logger;

    private readonly Dictionary<string, InteropType> returnTypeOverrides;
    private readonly Dictionary<string, InteropType> windowsOnlyReturnTypeOverrides;
    private readonly Dictionary<string, InteropType> systemvOnlyReturnTypeOverrides;
    private readonly Ioc ioc;

    public InteropSignatureTransform(Ioc ioc)
    {
        this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));

        returnTypeOverrides = new Dictionary<string, InteropType>
        {
            [Provider.GetTypeName(WellKnownName.Result)] = TypeRegistry.Int32,
            [Provider.GetTypeName(WellKnownName.PointerSize)] = TypeRegistry.VoidPtr
        };

        windowsOnlyReturnTypeOverrides = new Dictionary<string, InteropType>
        {
            [Provider.GetTypeName(WellKnownName.NativeLong)] = TypeRegistry.Int32,
            [Provider.GetTypeName(WellKnownName.NativeULong)] = TypeRegistry.UInt32
        };

        systemvOnlyReturnTypeOverrides = new Dictionary<string, InteropType>
        {
            [Provider.GetTypeName(WellKnownName.NativeLong)] = TypeRegistry.IntPtr,
            [Provider.GetTypeName(WellKnownName.NativeULong)] = TypeRegistry.UIntPtr
        };
    }

    public IDictionary<PlatformDetectionType, InteropMethodSignature> GetInteropSignatures(CsCallable callable)
    {
        var interopSignatures = new Dictionary<PlatformDetectionType, InteropMethodSignature>();
        var isFunction = callable is CsFunction;

        // On Windows x86 and x64, if we have a native member function signature with a struct return type, we need to do a by-ref return.
        // see https://github.com/dotnet/runtime/issues/10901
        // see https://github.com/dotnet/coreclr/pull/23145
        if (callable.IsReturnStructLarge && !isFunction)
        {
            interopSignatures.Add(
                PlatformDetectionType.Windows,
                GetNativeInteropSignatureWithForcedReturnBuffer(callable, false)
            );
            interopSignatures.Add(
                PlatformDetectionType.ItaniumSystemV,
                GetNativeInteropSignature(callable, false, PlatformDetectionType.ItaniumSystemV)
            );
        }
        else
        {
            var returnType = callable.ReturnValue.PublicType.QualifiedName;
            windowsOnlyReturnTypeOverrides.TryGetValue(returnType, out var windowsOverride);
            systemvOnlyReturnTypeOverrides.TryGetValue(returnType, out var systemvOverride);

            if (windowsOverride == systemvOverride)
                interopSignatures.Add(PlatformDetectionType.Any,
                                      GetNativeInteropSignature(callable, isFunction, PlatformDetectionType.Any));
            else
            {
                interopSignatures.Add(PlatformDetectionType.Windows,
                                      GetNativeInteropSignature(callable, isFunction,
                                                                PlatformDetectionType.Windows));
                interopSignatures.Add(PlatformDetectionType.ItaniumSystemV,
                                      GetNativeInteropSignature(callable, isFunction,
                                                                PlatformDetectionType.ItaniumSystemV));
            }
        }

        return interopSignatures;
    }

    private InteropMethodSignature GetNativeInteropSignatureWithForcedReturnBuffer(
        CsCallable callable, bool isFunction)
    {
        var cSharpInteropCalliSignature = new InteropMethodSignature
        {
            IsFunction = isFunction,
            CallingConvention = callable.CppCallingConvention,
            ForcedReturnBufferSig = true,
            ReturnType = TypeRegistry.VoidPtr,
            ParameterTypes = {new InteropMethodSignatureParameter(TypeRegistry.VoidPtr, callable.ReturnValue, "returnSlot")}
        };

        InitCalliSignatureParameters(callable, cSharpInteropCalliSignature);

        return cSharpInteropCalliSignature;
    }

    private InteropMethodSignature GetNativeInteropSignature(CsCallable callable, bool isFunction,
                                                             PlatformDetectionType platform)
    {
        // Tag if the method is a function
        var cSharpInteropCalliSignature = new InteropMethodSignature
        {
            IsFunction = isFunction,
            CallingConvention = callable.CppCallingConvention
        };

        InitSignatureWithReturnType(callable, cSharpInteropCalliSignature, platform);

        // Handle Parameters
        InitCalliSignatureParameters(callable, cSharpInteropCalliSignature);

        return cSharpInteropCalliSignature;
    }

    private void InitCalliSignatureParameters(CsCallable callable,
                                              InteropMethodSignature cSharpInteropCalliSignature)
    {
        foreach (var param in callable.Parameters)
        {
            var interopType = CoerceToBlittable(GetInteropTypeForParameter(param));

            if (interopType is null)
            {
                Logger.Error(LoggingCodes.InvalidMethodParameterType, "Invalid parameter {0} for method {1}",
                             param.PublicType.QualifiedName, callable.CppElement);
                continue;
            }

            cSharpInteropCalliSignature.ParameterTypes.Add(
                new InteropMethodSignatureParameter(interopType, param)
            );
        }
    }

    private void InitSignatureWithReturnType(CsCallable callable,
                                             InteropMethodSignature cSharpInteropCalliSignature,
                                             PlatformDetectionType platform)
    {
        var returnType = CoerceToBlittable(GetInteropTypeForReturnValue(callable.ReturnValue, platform));

        if (returnType is null)
        {
            Logger.Error(LoggingCodes.InvalidMethodReturnType, "Invalid return type {0} for method {1}",
                         callable.ReturnValue.PublicType.QualifiedName, callable.CppElement);
            returnType = callable.ReturnValue.PublicType.QualifiedName;
        }

        cSharpInteropCalliSignature.ReturnType = returnType;
    }

    private InteropType CoerceToBlittable(InteropType type) =>
        ioc.GeneratorConfig.UseFunctionPointersInVtbl && type is { TypeName: "char" } ? "ushort" : type;

    private InteropType GetInteropTypeForReturnValue(CsReturnValue returnValue, PlatformDetectionType platform)
    {
        // Handle Return Type parameter
        // MarshalType.Type == null, then check that it is a structure
        if (returnValue.PublicType is CsStruct or CsEnum)
        {
            var returnQualifiedName = returnValue.PublicType.QualifiedName;
            var platformSpecificReturnTypeOverrides = (platform & PlatformDetectionType.Windows) != 0
                                                          ? windowsOnlyReturnTypeOverrides
                                                          : systemvOnlyReturnTypeOverrides;

            if (returnTypeOverrides.TryGetValue(returnQualifiedName, out var interopType))
            {
                return interopType;
            }

            if (platformSpecificReturnTypeOverrides.TryGetValue(returnQualifiedName, out interopType))
            {
                return interopType;
            }

            return returnValue.HasNativeValueType
                       ? $"{returnValue.MarshalType.QualifiedName}.__Native"
                       : returnValue.MarshalType.QualifiedName;
        }

        if (returnValue.MarshalType is CsFundamentalType fundamentalReturn)
            return fundamentalReturn;

        if (returnValue.HasPointer)
            return returnValue.IsInterface ? TypeRegistry.IntPtr : TypeRegistry.VoidPtr;

        return null;
    }

    private InteropType GetInteropTypeForParameter(CsParameter param)
    {
        if (param.HasPointer)
            return TypeRegistry.VoidPtr;

        if (param.PublicType.IsWellKnownType(Provider, WellKnownName.PointerSize))
            return TypeRegistry.VoidPtr;

        if (param.MarshalType is CsFundamentalType marshalFundamental)
            return marshalFundamental switch
            {
                {IsIntPtr: true} => TypeRegistry.VoidPtr,
                _ => marshalFundamental
            };

        return param.PublicType switch
        {
            CsFundamentalType {IsIntPtr: true} => TypeRegistry.VoidPtr,
            CsFundamentalType publicFundamental => publicFundamental,
            CsStruct {HasMarshalType: true} csStruct => $"{csStruct.QualifiedName}.__Native",
            CsStruct csStruct => csStruct.QualifiedName,
            CsEnum csEnum => csEnum.UnderlyingType,
            _ => null
        };
    }
}