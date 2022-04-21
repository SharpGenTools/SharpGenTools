using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ShadowGenerator : MemberSingleCodeGeneratorBase<CsInterface>
{
    public override MemberDeclarationSyntax GenerateCode(CsInterface csElement)
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
                               GlobalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectVtbl),
                               Identifier("Vtbl")
                           )
                          .WithModifiers(
                               TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                           )
                          .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        var members = NewMemberList;

        if (csElement.AutoGenerateVtbl)
            members.Add(csElement, Generators.Vtbl);

        if (csElement.StaticShadowVtbl)
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
                                  Token(SyntaxKind.ReadOnlyKeyword))
                    )
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
              .WithModifiers(
                   ModelUtilities.VisibilityToTokenList(csElement.ShadowVisibility, SyntaxKind.PartialKeyword)
               )
              .WithBaseList(
                   BaseList(
                       SingletonSeparatedList<BaseTypeSyntax>(
                           SimpleBaseType(
                               csElement.Base != null
                                   ? IdentifierName(csElement.Base.ShadowName)
                                   : GlobalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectShadow)
                           )
                       )
                   )
               )
              .WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public ShadowGenerator(Ioc ioc) : base(ioc)
    {
    }
}