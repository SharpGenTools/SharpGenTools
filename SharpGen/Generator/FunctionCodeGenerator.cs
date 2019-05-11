using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SharpGen.Generator
{
    class FunctionCodeGenerator : IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax>
    {
        public FunctionCodeGenerator(IGeneratorRegistry generators)
        {
            Generators = generators;
        }

        public IGeneratorRegistry Generators { get; }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsFunction csElement)
        {
            foreach (var member in Generators.Callable.GenerateCode(csElement))
            {
                yield return member;
            }

            foreach (var sig in csElement.InteropSignatures)
            {
                yield return MethodDeclaration(ParseTypeName(sig.Value.ReturnType.TypeName), $"{csElement.CppElementName}{GeneratorHelpers.GetPlatformSpecificSuffix(sig.Key)}")
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
                                        })))))))
                    .WithParameterList(ParameterList(SeparatedList(
                        sig.Value.ParameterTypes.Select((param, i) =>
                            Parameter(Identifier($"param{i}"))
                                .WithType(ParseTypeName(param.TypeName))))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)); 
            }
        }

    }
}
