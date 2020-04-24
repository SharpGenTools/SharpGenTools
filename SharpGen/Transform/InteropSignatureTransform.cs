using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGen.Transform
{
    internal class InteropSignatureTransform
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
                { provider.GetTypeName(WellKnownName.Result), typeof(int) },
                { provider.GetTypeName(WellKnownName.PointerSize), typeof(void*) }
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

        public Dictionary<PlatformDetectionType, InteropMethodSignature> GetInteropSignatures(CsCallable callable, bool isFunction)
        {
            var interopSignatures = new Dictionary<PlatformDetectionType, InteropMethodSignature>();
            if (callable.IsReturnStructLarge)
            {
                var sigWithRetBuf = GetNativeInteropSignatureWithForcedReturnBuffer(callable, isFunction);
                interopSignatures.Add(PlatformDetectionType.IsWindows, sigWithRetBuf);
                interopSignatures.Add(PlatformDetectionType.IsItaniumSystemV, GetNativeInteropSignature(callable, isFunction, PlatformDetectionType.IsItaniumSystemV));
            }
            else
            {
                var returnType = callable.ReturnValue.PublicType.QualifiedName;
                windowsOnlyReturnTypeOverrides.TryGetValue(returnType, out var windowsOverride);
                systemvOnlyReturnTypeOverrides.TryGetValue(returnType, out var systemvOverride);

                if (windowsOverride == systemvOverride)
                    interopSignatures.Add(PlatformDetectionType.Any, GetNativeInteropSignature(callable, isFunction, PlatformDetectionType.Any));
                else
                {

                    interopSignatures.Add(PlatformDetectionType.IsWindows, GetNativeInteropSignature(callable, isFunction, PlatformDetectionType.IsWindows));
                    interopSignatures.Add(PlatformDetectionType.IsItaniumSystemV, GetNativeInteropSignature(callable, isFunction, PlatformDetectionType.IsItaniumSystemV));
                }
            }

            return interopSignatures;
        }

        private InteropMethodSignature GetNativeInteropSignatureWithForcedReturnBuffer(CsCallable callable, bool isFunction)
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

        /// <summary>
        /// Registers the native interop signature.
        /// </summary>
        /// <param name="callable">The cs method.</param>
        private InteropMethodSignature GetNativeInteropSignature(CsCallable callable, bool isFunction, PlatformDetectionType platform)
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

        private void InitCalliSignatureParameters(CsCallable callable, InteropMethodSignature cSharpInteropCalliSignature)
        {
            foreach (var param in callable.Parameters)
            {
                var interopType = GetInteropTypeForParameter(param);

                if (interopType == null)
                {
                    logger.Error(LoggingCodes.InvalidMethodParameterType, "Invalid parameter {0} for method {1}", param.PublicType.QualifiedName, callable.CppElement);
                }

                cSharpInteropCalliSignature.ParameterTypes.Add(interopType);
            }
        }

        private void InitSignatureWithReturnType(CsCallable callable, InteropMethodSignature cSharpInteropCalliSignature, PlatformDetectionType platform)
        {
            Debug.Assert((platform & (PlatformDetectionType.IsWindows | PlatformDetectionType.IsItaniumSystemV)) != (PlatformDetectionType.IsWindows | PlatformDetectionType.IsItaniumSystemV) || !callable.IsReturnStructLarge);
            var platformSpecificReturnTypeOverrides = (platform & PlatformDetectionType.IsWindows) != 0
                ? windowsOnlyReturnTypeOverrides
                : systemvOnlyReturnTypeOverrides;
            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (callable.ReturnValue.PublicType is CsStruct || callable.ReturnValue.PublicType is CsEnum)
            {
                var returnQualifiedName = callable.ReturnValue.PublicType.QualifiedName;
                if (returnTypeOverrides.TryGetValue(returnQualifiedName, out var interopType))
                {
                    cSharpInteropCalliSignature.ReturnType = interopType.NewType;
                    cSharpInteropCalliSignature.Flags |= interopType.SetFlags;
                }
                else if (platformSpecificReturnTypeOverrides.TryGetValue(returnQualifiedName, out interopType))
                {
                    cSharpInteropCalliSignature.ReturnType = interopType.NewType;
                    cSharpInteropCalliSignature.Flags |= interopType.SetFlags;
                }
                else if (callable.ReturnValue.HasNativeValueType)
                    cSharpInteropCalliSignature.ReturnType = $"{callable.ReturnValue.MarshalType.QualifiedName}.__Native";
                else
                    cSharpInteropCalliSignature.ReturnType = callable.ReturnValue.MarshalType.QualifiedName;
            }
            else if (callable.ReturnValue.MarshalType is CsFundamentalType fundamentalReturn)
            {
                cSharpInteropCalliSignature.ReturnType = fundamentalReturn.Type;
            }
            else if (callable.ReturnValue.HasPointer)
            {
                if (callable.ReturnValue.IsInterface)
                {
                    cSharpInteropCalliSignature.ReturnType = typeof(IntPtr);
                }
                else
                {
                    cSharpInteropCalliSignature.ReturnType = typeof(void*);
                }
            }
            else
            {
                cSharpInteropCalliSignature.ReturnType = callable.ReturnValue.PublicType.QualifiedName;
                logger.Error(LoggingCodes.InvalidMethodReturnType, "Invalid return type {0} for method {1}", callable.ReturnValue.PublicType.QualifiedName, callable.CppElement);
            }
        }

        private InteropType GetInteropTypeForParameter(CsParameter param)
        {
            InteropType interopType;
            var publicName = param.PublicType.QualifiedName;
            if (publicName == provider.GetTypeName(WellKnownName.PointerSize))
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
                if (type == typeof(IntPtr))
                    type = typeof(void*);
                interopType = type;
            }
            else if (param.PublicType is CsFundamentalType publicFundamental)
            {
                var type = publicFundamental.Type;
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
            }
            else if (param.PublicType is CsEnum csEnum)
            {
                interopType = csEnum.UnderlyingType.Type;
            }
            else
            {
                interopType = null;
            }

            return interopType;
        }
    }
}
