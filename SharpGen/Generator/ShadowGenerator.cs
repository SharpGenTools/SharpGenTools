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
    class ShadowGenerator : ICodeGenerator<CsInterface, MemberDeclarationSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly IGeneratorRegistry generators;

        public ShadowGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public MemberDeclarationSyntax GenerateCode(CsInterface csElement)
        {
            var shadowClassName = csElement.ShadowName.Split('.').Last();

            return ClassDeclaration(shadowClassName)
            .WithBaseList(
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            csElement.Base != null ?
                                (NameSyntax)IdentifierName(csElement.Base.ShadowName)
                                : globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectShadow)))))
            .WithMembers(
                List(
                    new MemberDeclarationSyntax[]{
                        generators.Vtbl.GenerateCode(csElement),
                        PropertyDeclaration(
                            globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectVtbl),
                            Identifier("Vtbl"))
                        .WithModifiers(
                            TokenList(
                                new []{
                                    Token(SyntaxKind.ProtectedKeyword),
                                    Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        Token(SyntaxKind.SemicolonToken)))))
                        .WithInitializer(
                            EqualsValueClause(
                                ObjectCreationExpression(
                                    IdentifierName(csElement.VtblName))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(0))))))))
                        .WithSemicolonToken(
                            Token(SyntaxKind.SemicolonToken))
                    }));
        }
    }
}
