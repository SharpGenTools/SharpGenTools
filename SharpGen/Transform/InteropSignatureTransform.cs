using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;

namespace SharpGen.Transform
{
    internal sealed class InteropSignatureTransform : IInteropSignatureTransform
    {
        public class SignatureInteropTypeOverride
        {
            public SignatureInteropTypeOverride(InteropType newType, InteropMethodSignatureFlags? setFlags = null)
            {
                NewType = newType ?? throw new ArgumentNullException(nameof(newType));

                if (setFlags.HasValue)
                    SetFlags = setFlags.Value;
            }

            public InteropType NewType { get; }
            public InteropMethodSignatureFlags SetFlags { get; } = InteropMethodSignatureFlags.None;

            public static implicit operator SignatureInteropTypeOverride(Type input) =>
                new SignatureInteropTypeOverride(input);
        }

        private readonly GlobalNamespaceProvider provider;
        private readonly Logger logger;
        private readonly Dictionary<string, SignatureInteropTypeOverride> returnTypeOverrides;
        private readonly Dictionary<string, SignatureInteropTypeOverride> windowsOnlyReturnTypeOverrides;
        private readonly Dictionary<string, SignatureInteropTypeOverride> systemvOnlyReturnTypeOverrides;

        public InteropSignatureTransform(GlobalNamespaceProvider provider, Logger logger)
        {
            this.provider = provider;
            this.logger = logger;

            returnTypeOverrides = new Dictionary<string, SignatureInteropTypeOverride>
            {
                {provider.GetTypeName(WellKnownName.Result), typeof(int)},
                {provider.GetTypeName(WellKnownName.PointerSize), typeof(void*)}
            };

            windowsOnlyReturnTypeOverrides = new Dictionary<string, SignatureInteropTypeOverride>
            {
                {
                    provider.GetTypeName(WellKnownName.NativeLong),
                    new SignatureInteropTypeOverride(typeof(int), InteropMethodSignatureFlags.CastToNativeLong)
                },
                {
                    provider.GetTypeName(WellKnownName.NativeULong),
                    new SignatureInteropTypeOverride(typeof(uint), InteropMethodSignatureFlags.CastToNativeULong)
                }
            };

            systemvOnlyReturnTypeOverrides = new Dictionary<string, SignatureInteropTypeOverride>
            {
                {
                    provider.GetTypeName(WellKnownName.NativeLong),
                    new SignatureInteropTypeOverride(typeof(IntPtr), InteropMethodSignatureFlags.CastToNativeLong)
                },
                {
                    provider.GetTypeName(WellKnownName.NativeULong),
                    new SignatureInteropTypeOverride(typeof(UIntPtr), InteropMethodSignatureFlags.CastToNativeULong)
                }
            };
        }

        public IDictionary<PlatformDetectionType, InteropMethodSignature> GetInteropSignatures(CsCallable callable)
        {
            var interopSignatures = new Dictionary<PlatformDetectionType, InteropMethodSignature>();
            var isFunction = callable is CsFunction;

            if (callable.IsReturnStructLarge)
            {
                interopSignatures.Add(
                    PlatformDetectionType.IsWindows,
                    GetNativeInteropSignatureWithForcedReturnBuffer(callable, false)
                );
                interopSignatures.Add(
                    PlatformDetectionType.IsItaniumSystemV,
                    GetNativeInteropSignature(callable, false, PlatformDetectionType.IsItaniumSystemV)
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
                    interopSignatures.Add(PlatformDetectionType.IsWindows,
                                          GetNativeInteropSignature(callable, isFunction,
                                                                    PlatformDetectionType.IsWindows));
                    interopSignatures.Add(PlatformDetectionType.IsItaniumSystemV,
                                          GetNativeInteropSignature(callable, isFunction,
                                                                    PlatformDetectionType.IsItaniumSystemV));
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
                CallingConvention = callable.CallingConvention,
                ForcedReturnBufferSig = true,
                ReturnType = typeof(void*),
                ParameterTypes = {typeof(void*)}
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
                CallingConvention = callable.CallingConvention
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
                var interopType = GetInteropTypeForParameter(provider, param);

                if (interopType == null)
                {
                    logger.Error(LoggingCodes.InvalidMethodParameterType, "Invalid parameter {0} for method {1}",
                                 param.PublicType.QualifiedName, callable.CppElement);
                    continue;
                }

                cSharpInteropCalliSignature.ParameterTypes.Add(
                    interopType
                );
            }
        }

        private void InitSignatureWithReturnType(CsCallable callable,
                                                 InteropMethodSignature cSharpInteropCalliSignature,
                                                 PlatformDetectionType platform)
        {
            InteropMethodSignatureFlags flags = default;

            var returnType = GetInteropTypeForReturnValue(callable.ReturnValue, platform, ref flags);

            if (returnType == null)
            {
                logger.Error(LoggingCodes.InvalidMethodReturnType, "Invalid return type {0} for method {1}",
                             callable.ReturnValue.PublicType.QualifiedName, callable.CppElement);
                returnType = callable.ReturnValue.PublicType.QualifiedName;
            }

            if (flags != default)
                cSharpInteropCalliSignature.Flags |= flags;

            cSharpInteropCalliSignature.ReturnType = returnType;
        }

        private InteropType GetInteropTypeForReturnValue(CsReturnValue returnValue,
                                                         PlatformDetectionType platform,
                                                         ref InteropMethodSignatureFlags flags)
        {
            var platformSpecificReturnTypeOverrides = (platform & PlatformDetectionType.IsWindows) != 0
                                                          ? windowsOnlyReturnTypeOverrides
                                                          : systemvOnlyReturnTypeOverrides;

            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (returnValue.PublicType is CsStruct || returnValue.PublicType is CsEnum)
            {
                var returnQualifiedName = returnValue.PublicType.QualifiedName;

                if (returnTypeOverrides.TryGetValue(returnQualifiedName, out var interopType))
                {
                    flags |= interopType.SetFlags;
                    return interopType.NewType;
                }

                if (platformSpecificReturnTypeOverrides.TryGetValue(returnQualifiedName, out interopType))
                {
                    flags |= interopType.SetFlags;
                    return interopType.NewType;
                }

                return returnValue.HasNativeValueType
                           ? $"{returnValue.MarshalType.QualifiedName}.__Native"
                           : returnValue.MarshalType.QualifiedName;
            }

            if (returnValue.MarshalType is CsFundamentalType fundamentalReturn)
                return fundamentalReturn.Type;

            if (returnValue.HasPointer)
                return returnValue.IsInterface ? typeof(IntPtr) : typeof(void*);

            return null;
        }

        private static InteropType GetInteropTypeForParameter(GlobalNamespaceProvider nsProvider, CsParameter param)
        {
            if (param.HasPointer)
                return typeof(void*);

            if (param.PublicType.QualifiedName == nsProvider.GetTypeName(WellKnownName.PointerSize))
                return typeof(void*);

            static Type IntPtrToVoidPtr(Type type) => type == typeof(IntPtr) ? typeof(void*) : type;

            if (param.MarshalType is CsFundamentalType marshalFundamental)
                return IntPtrToVoidPtr(marshalFundamental.Type);

            return param.PublicType switch
            {
                CsFundamentalType publicFundamental => IntPtrToVoidPtr(publicFundamental.Type),
                CsStruct {HasMarshalType: true} csStruct => $"{csStruct.QualifiedName}.__Native",
                CsStruct csStruct => csStruct.QualifiedName,
                CsEnum csEnum => csEnum.UnderlyingType.Type,
                _ => null
            };
        }
    }
}
