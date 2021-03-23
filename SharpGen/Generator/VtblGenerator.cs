using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class VtblGenerator : ICodeGenerator<CsInterface, MemberDeclarationSyntax>
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public VtblGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        public MemberDeclarationSyntax GenerateCode(CsInterface csElement)
        {
            var vtblClassName = csElement.VtblName.Split('.').Last();

            return ClassDeclaration(vtblClassName)
                  .WithModifiers(
                       ModelUtilities.VisibilityToTokenList(csElement.VtblVisibility, SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword)
                   )
                .WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                csElement.Base != null
                                    ? IdentifierName(csElement.Base.VtblName)
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
                                                                            IdentifierName(GetMethodDelegateName(method, platform)))
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

        internal static string GetMethodDelegateName(CsCallable csElement, PlatformDetectionType platform) =>
            csElement.Name + "Delegate" + GeneratorHelpers.GetPlatformSpecificSuffix(platform);
    }
}
