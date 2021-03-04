using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

            var vtblName = IdentifierName(csElement.VtblName);
            var vtblConstructor = ArgumentList(
                SingletonSeparatedList(
                    Argument(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)
                        )
                    )
                )
            );

            var vtblProperty = PropertyDeclaration(
                                   globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectVtbl),
                                   Identifier("Vtbl")
                               )
                              .WithModifiers(
                                   TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                               )
                              .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            List<MemberDeclarationSyntax> members = new();

            if (csElement.VtblName == csElement.DefaultVtblName || csElement.VtblName == csElement.DefaultVtblFullName)
            {
                members.Add(generators.Vtbl.GenerateCode(csElement));
            }

            if (csElement.StaticVtbl)
            {
                var vtblInstanceName = Identifier("VtblInstance");

                members.Add(
                    FieldDeclaration(
                            VariableDeclaration(vtblName)
                               .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator(vtblInstanceName)
                                           .WithInitializer(
                                                EqualsValueClause(
                                                    ImplicitObjectCreationExpression()
                                                       .WithArgumentList(
                                                            vtblConstructor
                                                        )
                                                )
                                            )
                                    )
                                )
                        )
                       .WithModifiers(
                            TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword),
                                      Token(SyntaxKind.ReadOnlyKeyword)))
                );

                vtblProperty = vtblProperty
                   .WithExpressionBody(
                        ArrowExpressionClause(IdentifierName(vtblInstanceName))
                    );
            }
            else
            {
                vtblProperty = vtblProperty
                              .WithAccessorList(
                                   AccessorList(
                                       SingletonList(
                                           AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                              .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                       )
                                   )
                               )
                              .WithInitializer(
                                   EqualsValueClause(
                                       ObjectCreationExpression(vtblName)
                                          .WithArgumentList(
                                               vtblConstructor
                                           )
                                   )
                               );
            }

            members.Add(vtblProperty);

            return ClassDeclaration(shadowClassName)
                  .WithBaseList(
                       BaseList(
                           SingletonSeparatedList<BaseTypeSyntax>(
                               SimpleBaseType(
                                   csElement.Base != null
                                       ? IdentifierName(csElement.Base.ShadowName)
                                       : globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectShadow)
                               )
                           )
                       )
                   )
                  .WithMembers(
                       new SyntaxList<MemberDeclarationSyntax>(members)
                   );
        }
    }
}