using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SharpGen.Generator
{
    class VtblGenerator : ICodeGenerator<CsInterface, MemberDeclarationSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly IGeneratorRegistry generators;

        public VtblGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public MemberDeclarationSyntax GenerateCode(CsInterface csElement)
        {
            var vtblClassName = csElement.VtblName.Split('.').Last();

            return ClassDeclaration(vtblClassName)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.ProtectedKeyword),
                        Token(SyntaxKind.UnsafeKeyword)))
                .WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                csElement.Base != null ?
                                    (NameSyntax)IdentifierName(csElement.Base.VtblName)
                                    : globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectVtbl)))))
                .WithMembers(
                    List(
                        new MemberDeclarationSyntax[]
                        {
                            ConstructorDeclaration(
                                Identifier(vtblClassName))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithParameterList(
                                ParameterList(
                                    SingletonSeparatedList(
                                        Parameter(
                                            Identifier("numberOfCallbackMethods"))
                                        .WithType(
                                            PredefinedType(
                                                Token(SyntaxKind.IntKeyword))))))
                            .WithInitializer(
                                ConstructorInitializer(
                                    SyntaxKind.BaseConstructorInitializer,
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                BinaryExpression(
                                                    SyntaxKind.AddExpression,
                                                    IdentifierName("numberOfCallbackMethods"),
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(csElement.Methods.Count()))))))))
                            .WithBody(
                                Block(csElement.Methods
                                        .OrderBy(method => method.Offset)
                                        .Select(method => GeneratorHelpers.GetPlatformSpecificStatements(globalNamespace, generators.Config, method.InteropSignatures.Keys,
                                            platform  =>
                                                ExpressionStatement(
                                                    InvocationExpression(
                                                        IdentifierName("AddMethod"))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SeparatedList(
                                                                new []
                                                                {
                                                                    Argument(
                                                                        ObjectCreationExpression(
                                                                            IdentifierName($"{method.Name}Delegate{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}"))
                                                                        .WithArgumentList(
                                                                            ArgumentList(
                                                                                SingletonSeparatedList(
                                                                                    Argument(
                                                                                        IdentifierName($"{method.Name}{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}")))))),
                                                                    Argument(
                                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                                            Literal((platform & PlatformDetectionType.IsWindows) != 0 ? method.WindowsOffset : method.Offset)))
                                                                }
                                                                ))))))))
                        }
                    .Concat(csElement.Methods
                                .SelectMany(method => generators.ShadowCallable.GenerateCode(method)))));
        }
    }
}
