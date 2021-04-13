using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.CppModel;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class NativeInvocationCodeGenerator : INativeCallCodeGenerator
    {
        private static readonly PointerTypeSyntax VoidPtr = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));
        private static readonly PointerTypeSyntax TripleVoidPtr = PointerType(PointerType(VoidPtr));

        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public NativeInvocationCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        private IEnumerable<(ArgumentSyntax Argument, TypeSyntax Type)> IterateNativeArguments(CsCallable callable,
            InteropMethodSignature interopSig)
        {
            if (callable is CsMethod)
            {
                var ptr = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName("_nativePointer")
                );

                yield return (Argument(ptr), VoidPtr);
            }

            (ArgumentSyntax, TypeSyntax) ParameterSelector(InteropMethodSignatureParameter param)
            {
                var csElement = param.Item;
                var marshaller = generators.Marshalling.GetMarshaller(csElement);
                return (marshaller.GenerateNativeArgument(csElement), param.InteropTypeSyntax);
            }

            foreach (var parameter in interopSig.ParameterTypes)
                yield return ParameterSelector(parameter);
        }

        public ExpressionSyntax GenerateCall(CsCallable callable, PlatformDetectionType platform,
                                             InteropMethodSignature interopSig)
        {
            var arguments = IterateNativeArguments(callable, interopSig).ToArray();

            ElementAccessExpressionSyntax vtblAccess = null;

            if (callable is CsMethod method)
            {
                var windowsOffsetExpression =
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.WindowsOffset));
                var nonWindowsOffsetExpression =
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset));

                ExpressionSyntax vtableOffsetExpression;
                if ((platform & PlatformDetectionType.Any) == PlatformDetectionType.Any &&
                    method.Offset != method.WindowsOffset)
                {
                    vtableOffsetExpression = ConditionalExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
                            IdentifierName("Is" + nameof(PlatformDetectionType.Windows))),
                        windowsOffsetExpression,
                        nonWindowsOffsetExpression);
                }
                else if ((platform & PlatformDetectionType.Windows) != 0)
                {
                    vtableOffsetExpression = windowsOffsetExpression;
                }
                else
                {
                    vtableOffsetExpression = nonWindowsOffsetExpression;
                }

                vtblAccess = ElementAccessExpression(
                    ParenthesizedExpression(
                        PrefixUnaryExpression(
                            SyntaxKind.PointerIndirectionExpression,
                            CastExpression(
                                TripleVoidPtr,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName("_nativePointer")
                                )
                            )
                        )
                    ),
                    BracketedArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                method.CustomVtbl
                                    ? MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName($"{callable.Name}__vtbl_index")
                                    )
                                    : vtableOffsetExpression
                            )
                        )
                    )
                );
            }

            CastExpressionSyntax FnPtrCall()
            {
                var fnptrParameters = arguments
                                     .Select(x => x.Type)
                                     .Append(ParseTypeName(interopSig.ReturnType.TypeName))
                                     .Select(FunctionPointerParameter);

                return CastExpression(
                    FunctionPointerType(
                        FunctionPointerCallingConvention(
                            Token(SyntaxKind.UnmanagedKeyword),
                            FunctionPointerUnmanagedCallingConventionList(
                                SingletonSeparatedList(
                                    FunctionPointerUnmanagedCallingConvention(
                                        Identifier(callable.CppCallingConvention.ToCallConvShortName())
                                    )
                                )
                            )
                        ),
                        FunctionPointerParameterList(SeparatedList(fnptrParameters))
                    ),
                    ParenthesizedExpression(vtblAccess)
                );
            }

            ExpressionSyntax what = callable switch
            {
                CsFunction => IdentifierName(
                    callable.CppElementName + GeneratorHelpers.GetPlatformSpecificSuffix(platform)
                ),
                CsMethod => ParenthesizedExpression(FnPtrCall()),
                _ => throw new ArgumentOutOfRangeException()
            };

            ExpressionSyntax call = InvocationExpression(
                what,
                ArgumentList(SeparatedList(arguments.Select(x => x.Argument)))
            );

            if (interopSig.CastToNativeLong)
                call = CastExpression(globalNamespace.GetTypeNameSyntax(WellKnownName.NativeLong), call);

            if (interopSig.CastToNativeULong)
                call = CastExpression(globalNamespace.GetTypeNameSyntax(WellKnownName.NativeULong), call);

            if (interopSig.ForcedReturnBufferSig || !callable.HasReturnType)
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