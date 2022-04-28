using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

public sealed partial class SharpGenModuleGenerator
{
    private static readonly NameSyntax ConditionalAttribute = ParseName("System.Diagnostics.Conditional");
    private static readonly NameSyntax AttributeUsage = ParseName("System.AttributeUsage");
    private static readonly NameSyntax AttributeTargets = ParseName("System.AttributeTargets");
    private static readonly NameSyntax Attribute = ParseName("System.Attribute");

    private static readonly AttributeSyntax DebugConditionalAttribute = SyntaxFactory.Attribute(
        ConditionalAttribute,
        AttributeArgumentList(
            SingletonSeparatedList(
                AttributeArgument(
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("SHARPGEN_ATTRIBUTE_DEBUG"))
                )
            )
        )
    );

    private static readonly AttributeSyntax MethodAttributeUsage = SyntaxFactory.Attribute(
        AttributeUsage,
        AttributeArgumentList(
            SeparatedList<AttributeArgumentSyntax>(
                new SyntaxNodeOrToken[]
                {
                    AttributeArgument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            AttributeTargets, IdentifierName("Method")
                        )
                    ),
                    Token(SyntaxKind.CommaToken),
                    AttributeArgument(LiteralExpression(SyntaxKind.FalseLiteralExpression))
                       .WithNameEquals(NameEquals(IdentifierName("Inherited")))
                }
            )
        )
    );

    private static void GenerateUtilities(GeneratorExecutionContext context)
    {
        List<MemberDeclarationSyntax> attributes = new(1);

        var moduleInitType = context.Compilation.GetTypeByMetadataName(ModuleInitializerAttributeName);
        if (moduleInitType is not { IsReferenceType: true, IsGenericType: false } ||
            !context.Compilation.IsSymbolAccessibleWithin(moduleInitType, context.Compilation.Assembly))
            attributes.Add(ModuleInitializerAttribute);

        if (attributes.Count == 0)
            return;

        context.AddSource(
            "SourceGeneratorUtilities.g.cs",
            SourceText.From(GenerateCompilationUnit(attributes).ToString(), Encoding.UTF8)
        );
    }

    private static NamespaceDeclarationSyntax ModuleInitializerAttribute =>
        NamespaceDeclaration(
                QualifiedName(
                    QualifiedName(IdentifierName("System"), IdentifierName("Runtime")),
                    IdentifierName("CompilerServices")
                )
            )
           .AddMembers(
                ClassDeclaration("ModuleInitializerAttribute")
                   .AddAttributeLists(
                        AttributeList(SingletonSeparatedList(MethodAttributeUsage)),
                        AttributeList(SingletonSeparatedList(DebugConditionalAttribute))
                    )
                   .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                   .AddBaseListTypes(SimpleBaseType(Attribute))
                   .AddMembers(
                        ConstructorDeclaration(Identifier("ModuleInitializerAttribute"))
                           .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                           .WithBody(Block())
                    )
            );
}