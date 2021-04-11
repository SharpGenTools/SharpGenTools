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
            public SignatureInteropTypeOverride(CsFundamentalType newType, InteropMethodSignatureFlags? setFlags = null)
            {
                NewType = newType ?? throw new ArgumentNullException(nameof(newType));

                if (setFlags.HasValue)
                    SetFlags = setFlags.Value;
            }

            public InteropType NewType { get; }
            public InteropMethodSignatureFlags SetFlags { get; } = InteropMethodSignatureFlags.None;

            public static implicit operator SignatureInteropTypeOverride(CsFundamentalType input) => new(input);
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
                {
                    provider.GetTypeName(WellKnownName.Result),
                    TypeRegistry.Int32
                },
                {
                    provider.GetTypeName(WellKnownName.PointerSize),
                    TypeRegistry.VoidPtr
                }
            };

            const InteropMethodSignatureFlags castToNativeLong = InteropMethodSignatureFlags.CastToNativeLong;
            const InteropMethodSignatureFlags castToNativeULong = InteropMethodSignatureFlags.CastToNativeULong;

            windowsOnlyReturnTypeOverrides = new Dictionary<string, SignatureInteropTypeOverride>
            {
                {
                    provider.GetTypeName(WellKnownName.NativeLong),
                    new SignatureInteropTypeOverride(TypeRegistry.Int32, castToNativeLong)
                },
                {
                    provider.GetTypeName(WellKnownName.NativeULong),
                    new SignatureInteropTypeOverride(TypeRegistry.UInt32, castToNativeULong)
                }
            };

            systemvOnlyReturnTypeOverrides = new Dictionary<string, SignatureInteropTypeOverride>
            {
                {
                    provider.GetTypeName(WellKnownName.NativeLong),
                    new SignatureInteropTypeOverride(TypeRegistry.IntPtr, castToNativeLong)
                },
                {
                    provider.GetTypeName(WellKnownName.NativeULong),
                    new SignatureInteropTypeOverride(TypeRegistry.UIntPtr, castToNativeULong)
                }
            };
        }

        public IDictionary<PlatformAbi, InteropMethodSignature> GetInteropSignatures(CsCallable callable)
        {
            var interopSignatures = new Dictionary<PlatformAbi, InteropMethodSignature>();
            var isFunction = callable is CsFunction;

            // On Windows x86 and x64, if we have a native member function signature with a struct return type, we need to do a by-ref return.
            // see https://github.com/dotnet/runtime/issues/10901
            // see https://github.com/dotnet/coreclr/pull/23145
            if (callable.IsReturnStructLarge && !isFunction)
            {
                interopSignatures.Add(
                    PlatformAbi.Windows,
                    GetNativeInteropSignatureWithForcedReturnBuffer(callable, false)
                );
                interopSignatures.Add(
                    PlatformAbi.ItaniumSystemV,
                    GetNativeInteropSignature(callable, false, PlatformAbi.ItaniumSystemV)
                );
            }
            else
            {
                var returnType = callable.ReturnValue.PublicType.QualifiedName;
                windowsOnlyReturnTypeOverrides.TryGetValue(returnType, out var windowsOverride);
                systemvOnlyReturnTypeOverrides.TryGetValue(returnType, out var systemvOverride);

                if (windowsOverride == systemvOverride)
                    interopSignatures.Add(PlatformAbi.Any,
                                          GetNativeInteropSignature(callable, isFunction, PlatformAbi.Any));
                else
                {
                    interopSignatures.Add(PlatformAbi.Windows,
                                          GetNativeInteropSignature(callable, isFunction,
                                                                    PlatformAbi.Windows));
                    interopSignatures.Add(PlatformAbi.ItaniumSystemV,
                                          GetNativeInteropSignature(callable, isFunction,
                                                                    PlatformAbi.ItaniumSystemV));
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
                                                                 PlatformAbi platform)
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
                var interopType = GetInteropTypeForParameter(provider, param);

                if (interopType == null)
                {
                    logger.Error(LoggingCodes.InvalidMethodParameterType, "Invalid parameter {0} for method {1}",
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
                                                 PlatformAbi platform)
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
                                                         PlatformAbi platform,
                                                         ref InteropMethodSignatureFlags flags)
        {
            var platformSpecificReturnTypeOverrides = (platform & PlatformAbi.Windows) != 0
                                                          ? windowsOnlyReturnTypeOverrides
                                                          : systemvOnlyReturnTypeOverrides;

            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (returnValue.PublicType is CsStruct or CsEnum)
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
                return fundamentalReturn;

            if (returnValue.HasPointer)
                return returnValue.IsInterface ? TypeRegistry.IntPtr : TypeRegistry.VoidPtr;

            return null;
        }

        private static InteropType GetInteropTypeForParameter(GlobalNamespaceProvider nsProvider, CsParameter param)
        {
            if (param.HasPointer)
                return TypeRegistry.VoidPtr;

            if (param.PublicType.IsWellKnownType(nsProvider, WellKnownName.PointerSize))
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
}
