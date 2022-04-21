using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class FunctionImportCodeGenerator : MemberPlatformMultiCodeGeneratorBase<CsFunction>
{
    private static readonly SyntaxTokenList ModifierList = TokenList(
        Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.UnsafeKeyword),
        Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ExternKeyword)
    );

    private static readonly QualifiedNameSyntax DllImportName = QualifiedName(
        QualifiedName(QualifiedName(IdentifierName("System"), IdentifierName("Runtime")),
                      IdentifierName("InteropServices")), IdentifierName("DllImportAttribute")
    );

    public FunctionImportCodeGenerator(Ioc ioc) : base(ioc)
    {
    }

    public override IEnumerable<PlatformDetectionType> GetPlatforms(CsFunction csElement) =>
        csElement.InteropSignatures.Keys;

    public override IEnumerable<MemberDeclarationSyntax> GenerateCode(
        CsFunction csElement, PlatformDetectionType platform)
    {
        var sig = csElement.InteropSignatures[platform];
        yield return MethodDeclaration(
                         sig.ReturnTypeSyntax,
                         $"{csElement.CppElementName}{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}"
                     )
                    .WithModifiers(ModifierList)
                    .WithAttributeLists(
                         SingletonList(
                             AttributeList(
                                 SingletonSeparatedList(
                                     Attribute(
                                             DllImportName)
                                        .WithArgumentList(
                                             AttributeArgumentList(
                                                 SeparatedList(
                                                     new[]
                                                     {
                                                         AttributeArgument(IdentifierName(csElement.DllName)),
                                                         AttributeArgument(
                                                                 LiteralExpression(
                                                                     SyntaxKind.StringLiteralExpression,
                                                                     Literal(csElement.CppElementName)))
                                                            .WithNameEquals(
                                                                 NameEquals(IdentifierName("EntryPoint"))),
                                                         AttributeArgument(
                                                                 ModelUtilities
                                                                    .GetManagedCallingConventionExpression(
                                                                         csElement.CppCallingConvention))
                                                            .WithNameEquals(
                                                                 NameEquals(IdentifierName("CallingConvention")))
                                                     })))))))
                    .WithParameterList(
                         ParameterList(
                             SeparatedList(
                                 sig.ParameterTypes.Select(
                                     param => Parameter(Identifier(param.Name))
                                        .WithType(param.InteropTypeSyntax)))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}