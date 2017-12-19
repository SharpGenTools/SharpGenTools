using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
namespace SharpGen.Generator
{
    class FunctionCodeGenerator : IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax>
    {

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsFunction csElement)
        {
            var interopFunction = csElement.Interop;

            foreach (var member in Generators.Callable.GenerateCode(csElement))
            {
                yield return member;
            }

            yield return MethodDeclaration(ParseTypeName(interopFunction.ReturnType.TypeName), $"{interopFunction.Name}_")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.UnsafeKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.ExternKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(
                            QualifiedName(
                                QualifiedName(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("Runtime")),
                                    IdentifierName("InteropServices")),
                                IdentifierName("DllImportAttribute")))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SeparatedList(
                                    new[]
                                    {
                                        AttributeArgument(
                                            IdentifierName(csElement.DllName)),
                                        AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(csElement.CppElementName)))
                                        .WithNameEquals(
                                            NameEquals(
                                                IdentifierName("EntryPoint"))),
                                        AttributeArgument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("System"),
                                                            IdentifierName("Runtime")),
                                                        IdentifierName("InteropServices")),
                                                    IdentifierName("CallingConvention")),
                                                IdentifierName(csElement.CallingConvention)))
                                        .WithNameEquals(
                                            NameEquals(
                                                IdentifierName("CallingConvention")))
                                    })))))));
        }

    }
}
