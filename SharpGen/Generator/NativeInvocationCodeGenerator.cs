using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGen.Generator
{
    class NativeInvocationCodeGenerator : ICodeGenerator<CsCallable, ExpressionSyntax>
    {
        public NativeInvocationCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
        }

        readonly GlobalNamespaceProvider globalNamespace;

        public IGeneratorRegistry Generators { get; }

        private ExpressionSyntax GetCastedReturn(ExpressionSyntax invocation, CsReturnValue returnValue, bool largeReturn)
        {
            var fundamentalPublic = returnValue.PublicType as CsFundamentalType;

            if (returnValue.IsBoolToInt)
                return BinaryExpression(SyntaxKind.NotEqualsExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                    invocation);
            if (returnValue.PublicType is CsInterface)
                return ObjectCreationExpression(ParseTypeName(returnValue.PublicType.QualifiedName),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(QualifiedName(IdentifierName("System"), IdentifierName("IntPtr")), invocation)))),
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression));
            if (fundamentalPublic?.Type == typeof(string))
            {
                var marshalMethodName = "PtrToString" + (returnValue.IsWideChar ? "Uni" : "Ansi");
                return InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal), IdentifierName(marshalMethodName)),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    invocation
                                    ))));
            }

            // If this is not null, the return type of the invocation differs from the public type
            if (returnValue.MarshalType != returnValue.PublicType && !largeReturn && returnValue.PublicType.QualifiedName != "void") 
            {
                return CheckedExpression(
                            SyntaxKind.UncheckedExpression,
                            CastExpression(
                                ParseTypeName(returnValue.PublicType.QualifiedName),
                                ParenthesizedExpression(invocation)));
            }

            return invocation;
        }
        
        public ExpressionSyntax GenerateCode(CsCallable callable)
        {
            var arguments = new List<ArgumentSyntax>();

            if (!(callable is CsFunction))
            {
                arguments.Add(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName("_nativePointer"))));
            }

            if (callable.IsReturnStructLarge)
            {
                if (callable.ReturnValue.PublicType is CsStruct returnStruct && returnStruct.HasMarshalType)
                {
                    arguments.Add(Argument(CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                            IdentifierName("__result__native")))));
                }
                else
                {
                    arguments.Add(Argument(CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                            IdentifierName("__result__")))));
                }
               
            }

            arguments.AddRange(callable.Parameters.Select(param => Generators.Argument.GenerateCode(param)));

            if (callable is CsMethod method)
            {
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
                                (ExpressionSyntax)MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName($"{callable.Name}__vtbl_index"))
                                : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset))
                                )
                            )))));
            }

            return GetCastedReturn(
                InvocationExpression(
                    IdentifierName(callable is CsFunction ?
                        callable.CppElementName + "_"
                    : callable.GetParent<CsAssembly>().QualifiedName + ".LocalInterop." + callable.Interop.Name),
                    ArgumentList(SeparatedList(arguments))),
                callable.ReturnValue,
                callable.IsReturnStructLarge
            );
        }

    }
}
