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
    class NativeInvocationCodeGenerator : ICodeGenerator<CsMethod, ExpressionSyntax>
    {
        GlobalNamespaceProvider GlobalNamespace;

        private static ExpressionSyntax GetCastedReturn(ExpressionSyntax invocation, CsMarshalBase returnType)
        {
            if (returnType.PublicType.Type != null && returnType.PublicType.Type == typeof(bool))
                return BinaryExpression(SyntaxKind.NotEqualsExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                    invocation);
            if (returnType.PublicType is CsInterface)
                return ObjectCreationExpression(ParseTypeName(returnType.PublicType.QualifiedName),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(QualifiedName(IdentifierName("System"), IdentifierName("IntPtr")), invocation)))),
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression));
            if (returnType.PublicType.Type == typeof(string))
            {
                var marshalMethodName = "PtrToString" + (returnType.IsWideChar ? "Uni" : "Ansi");
                return InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName("System.Runtime.InteropServices.Marshal"), IdentifierName(marshalMethodName)),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    invocation
                                    ))));
            }
            return invocation;
        }
        
        public ExpressionSyntax GenerateCode(CsMethod method)
        {
            var arguments = new List<ArgumentSyntax>();

            if (method.IsReturnStructLarge)
            {
                arguments.Add(Argument(CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            IdentifierName("__result__")))));
            }

            arguments.AddRange(method.Parameters.Select(param => Generators.Argument.GenerateCode(param)));

            if (!(method is CsFunction))
            {
                arguments.Add(Argument(
                    ElementAccessExpression(
                        ParenthesizedExpression(
                            PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                                CastExpression(PointerType(PointerType(PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))),
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName("nativePointer"))))),
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(method.CustomVtbl ?
                                (ExpressionSyntax)MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName($"{method.Name}__vtbl_index"))
                                : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset))
                                )
                            )))));
            }

            return GetCastedReturn(
                InvocationExpression(
                    IdentifierName(method is CsFunction ?
                        method.CppElementName + "_"
                    : method.Assembly.QualifiedName + ".LocalInterop." + method.Interop.Name),
                    ArgumentList(SeparatedList(arguments))),
                method.ReturnType
            );
        }

    }
}
