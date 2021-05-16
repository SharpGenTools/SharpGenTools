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
    internal sealed class NativeInvocationCodeGenerator : CodeGeneratorBase, INativeCallCodeGenerator
    {
        private static readonly PointerTypeSyntax VoidPtr = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));
        private static readonly PointerTypeSyntax TripleVoidPtr = PointerType(PointerType(VoidPtr));

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
                return (GetMarshaller(csElement).GenerateNativeArgument(csElement), param.InteropTypeSyntax);
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
                            GlobalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
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
                            GeneratorHelpers.CastExpression(
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

            ExpressionSyntax FnPtrCall()
            {
                var fnptrParameters = arguments
                                     .Select(x => x.Type)
                                     .Append(ParseTypeName(interopSig.ReturnType.TypeName))
                                     .Select(FunctionPointerParameter);

                return GeneratorHelpers.CastExpression(
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
                    vtblAccess
                );
            }

            var what = callable switch
            {
                CsFunction => IdentifierName(
                    callable.CppElementName + GeneratorHelpers.GetPlatformSpecificSuffix(platform)
                ),
                CsMethod => GeneratorHelpers.WrapInParentheses(FnPtrCall()),
                _ => throw new ArgumentOutOfRangeException()
            };

            ExpressionSyntax call = InvocationExpression(
                what,
                ArgumentList(SeparatedList(arguments.Select(x => x.Argument)))
            );

            if (interopSig.CastToNativeLong)
                call = CastExpression(GlobalNamespace.GetTypeNameSyntax(WellKnownName.NativeLong), call);

            if (interopSig.CastToNativeULong)
                call = CastExpression(GlobalNamespace.GetTypeNameSyntax(WellKnownName.NativeULong), call);

            if (interopSig.ForcedReturnBufferSig || !callable.HasReturnType)
                return call;

            return AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                GetMarshaller(callable.ReturnValue).GeneratesMarshalVariable(callable.ReturnValue)
                    ? MarshallerBase.GetMarshalStorageLocation(callable.ReturnValue)
                    : IdentifierName(callable.ReturnValue.Name),
                call
            );
        }

        public NativeInvocationCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}