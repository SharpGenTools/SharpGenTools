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
    internal sealed class NativeInvocationCodeGenerator : ExpressionPlatformSingleCodeGeneratorBase<CsCallable>
    {
        private static readonly ArgumentSyntax NativePointerArgument = Argument(IdentifierName("NativePointer"));

        private IEnumerable<(ArgumentSyntax Argument, TypeSyntax Type)> IterateNativeArguments(bool isMethod,
                                                                                               InteropMethodSignature interopSig)
        {
            if (isMethod)
                yield return (NativePointerArgument, GeneratorHelpers.IntPtrType);

            (ArgumentSyntax, TypeSyntax) ParameterSelector(InteropMethodSignatureParameter param)
            {
                var csElement = param.Item;
                return (GetMarshaller(csElement).GenerateNativeArgument(csElement), param.InteropTypeSyntax);
            }

            foreach (var parameter in interopSig.ParameterTypes)
                yield return ParameterSelector(parameter);
        }

        public override IEnumerable<PlatformDetectionType> GetPlatforms(CsCallable csElement) =>
            csElement.InteropSignatures.Keys;

        protected override ExpressionSyntax Generate(CsCallable callable, PlatformDetectionType platform)
        {
            var interopSig = callable.InteropSignatures[platform];
            var interopReturnType = interopSig.ReturnTypeSyntax;
            var arguments = IterateNativeArguments(callable is CsMethod, interopSig).ToArray();

            var vtblAccess = callable switch
            {
                CsMethod method => ElementAccessExpression(
                    ThisExpression(),
                    BracketedArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                method.CustomVtbl
                                    ? MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName($"{callable.Name}__vtbl_index")
                                    )
                                    : method.VTableOffsetExpression(platform)
                            )
                        )
                    )
                ),
                _ => null
            };

            ExpressionSyntax FnPtrCall()
            {
                var fnptrParameters = arguments
                                     .Select(x => x.Type)
                                     .Append(interopReturnType)
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

            if (interopSig.ForcedReturnBufferSig || !callable.HasReturnType)
                return call;

            var generatesMarshalVariable = GetMarshaller(callable.ReturnValue).GeneratesMarshalVariable(callable.ReturnValue);
            var publicTypeSyntax = ReverseCallablePrologCodeGenerator.GetPublicType(callable.ReturnValue);
            if (callable.HasReturnTypeValue && !generatesMarshalVariable && !publicTypeSyntax.IsEquivalentTo(interopSig.ReturnTypeSyntax))
                call = CastExpression(publicTypeSyntax, call);

            return AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                generatesMarshalVariable
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