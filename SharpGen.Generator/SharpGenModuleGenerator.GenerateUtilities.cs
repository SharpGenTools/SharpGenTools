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
}