using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Config;
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

            // Default: at least protected to enable inheritance.
            var vtblVisibility = csElement.VtblVisibility ?? Visibility.ProtectedInternal;

            StatementSyntax VtblMethodSelector(CsMethod method)
            {
                StatementSyntax MethodBuilder(PlatformDetectionType platform)
                {
                    var arguments = new[]
                    {
                        Argument(
                            ObjectCreationExpression(IdentifierName(GetMethodDelegateName(method, platform)))
                               .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(
                                                    $"{method.Name}{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}"
                                                )
                                            )
                                        )
                                    )
                                )
                        ),
                        Argument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal((platform & PlatformDetectionType.IsWindows) != 0
                                            ? method.WindowsOffset
                                            : method.Offset)
                            )
                        )
                    };

                    return ExpressionStatement(
                        InvocationExpression(IdentifierName("AddMethod"))
                           .WithArgumentList(ArgumentList(SeparatedList(arguments)))
                    );
                }

                return GeneratorHelpers.GetPlatformSpecificStatements(globalNamespace, generators.Config,
                                                                      method.InteropSignatures.Keys, MethodBuilder);
            }

            List<MemberDeclarationSyntax> members = new()
            {
                ConstructorDeclaration(Identifier(vtblClassName))
                   .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                   .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList(
                                Parameter(Identifier("numberOfCallbackMethods"))
                                   .WithType(PredefinedType(Token(SyntaxKind.IntKeyword)))
                            )
                        )
                    )
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
                                                Literal(csElement.MethodList.Count)
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                   .WithBody(
                        Block(
                            csElement.Methods
                                     .OrderBy(method => method.Offset)
                                     .Select(VtblMethodSelector)
                        )
                    )
            };

            members.AddRange(csElement.Methods.SelectMany(method => generators.ShadowCallable.GenerateCode(method)));

            return ClassDeclaration(vtblClassName)
                  .WithModifiers(
                       ModelUtilities.VisibilityToTokenList(vtblVisibility, SyntaxKind.UnsafeKeyword,
                                                            SyntaxKind.PartialKeyword)
                   )
                  .WithBaseList(
                       BaseList(
                           SingletonSeparatedList<BaseTypeSyntax>(
                               SimpleBaseType(
                                   csElement.Base != null
                                       ? IdentifierName(csElement.Base.VtblName)
                                       : globalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectVtbl)
                               )
                           )
                       )
                   )
                  .WithMembers(List(members));
        }

        internal static string GetMethodDelegateName(CsCallable csElement, PlatformDetectionType platform) =>
            csElement.Name + "Delegate" + GeneratorHelpers.GetPlatformSpecificSuffix(platform);
    }
}