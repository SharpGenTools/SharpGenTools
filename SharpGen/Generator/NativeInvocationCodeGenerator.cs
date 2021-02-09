using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class NativeInvocationCodeGenerator : INativeCallCodeGenerator
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public NativeInvocationCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        public ExpressionSyntax GenerateCall(CsCallable callable, PlatformDetectionType platform,
                                             InteropMethodSignature interopSig)
        {
            var arguments = new List<ArgumentSyntax>();

            if (callable is CsMethod)
            {
                arguments.Add(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName("_nativePointer"))));
            }

            var isForcedReturnBufferSig = interopSig.ForcedReturnBufferSig;

            if (isForcedReturnBufferSig)
            {
                arguments.Add(generators.Marshalling.GetMarshaller(callable.ReturnValue).GenerateNativeArgument(callable.ReturnValue)); 
            }

            arguments.AddRange(callable.Parameters.Select(param => generators.Marshalling.GetMarshaller(param).GenerateNativeArgument(param)));

            if (callable is CsMethod method)
            {
                var windowsOffsetExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.WindowsOffset));
                var nonWindowsOffsetExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset));
                ExpressionSyntax vtableOffsetExpression;
                if ((platform & PlatformDetectionType.Any) == PlatformDetectionType.Any
                    && method.Offset != method.WindowsOffset)
                {
                    vtableOffsetExpression = ConditionalExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
                            IdentifierName(PlatformDetectionType.IsWindows.ToString())),
                        windowsOffsetExpression,
                        nonWindowsOffsetExpression);
                }
                else if ((platform & PlatformDetectionType.IsWindows) != 0)
                {
                    vtableOffsetExpression = windowsOffsetExpression;
                }
                else
                {
                    vtableOffsetExpression = nonWindowsOffsetExpression;
                }
                arguments.Add(Argument(
                    ElementAccessExpression(
                        ParenthesizedExpression(
                            PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                                CastExpression(PointerType(PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))),
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName("_nativePointer"))))),
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(method.CustomVtbl ?
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName($"{callable.Name}__vtbl_index"))
                                : vtableOffsetExpression
                                )
                            )))));
            }

            ExpressionSyntax call = InvocationExpression(
                    IdentifierName(callable is CsFunction ?
                        callable.CppElementName + GeneratorHelpers.GetPlatformSpecificSuffix(platform)
                    : "LocalInterop." + interopSig.Name),
                    ArgumentList(SeparatedList(arguments)));

            if (interopSig.CastToNativeLong)
                call = CastExpression(globalNamespace.GetTypeNameSyntax(WellKnownName.NativeLong), call);
            
            if (interopSig.CastToNativeULong)
                call = CastExpression(globalNamespace.GetTypeNameSyntax(WellKnownName.NativeULong), call);

            if (isForcedReturnBufferSig || !callable.HasReturnType)
                return call;

            var generatesMarshalVariable = generators.Marshalling
                                                     .GetMarshaller(callable.ReturnValue)
                                                     .GeneratesMarshalVariable(callable.ReturnValue);

            return AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                generatesMarshalVariable
                    ? MarshallerBase.GetMarshalStorageLocation(callable.ReturnValue)
                    : IdentifierName(callable.ReturnValue.Name),
                call
            );
        }

    }
}
