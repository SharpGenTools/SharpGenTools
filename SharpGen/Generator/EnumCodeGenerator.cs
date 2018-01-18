using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class EnumCodeGenerator : MemberCodeGeneratorBase<CsEnum>
    {
        public EnumCodeGenerator(IDocumentationLinker documentation) : base(documentation)
        {
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsEnum csElement)
        {
            var enumDecl = EnumDeclaration(csElement.Name);
            enumDecl = enumDecl.WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                .WithBaseList(
                    BaseList().
                        WithTypes(SingletonSeparatedList<BaseTypeSyntax>
                (
                    SimpleBaseType(ParseTypeName(csElement.UnderlyingType.FullName))
                )))
                .AddMembers(csElement.EnumItems.Select(item =>
                {
                    var itemDecl = EnumMemberDeclaration(item.Name);

                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        itemDecl = itemDecl.WithEqualsValue(
                        EqualsValueClause(
                            CheckedExpression(
                                SyntaxKind.UncheckedExpression,
                                CastExpression(
                                    ParseTypeName(csElement.UnderlyingType.FullName),
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(int.Parse(item.Value)))))));
                    }
                    return itemDecl
                        .WithLeadingTrivia(Trivia(GenerateDocumentationTrivia(item)));
                }).ToArray()).WithLeadingTrivia(Trivia(GenerateDocumentationTrivia(csElement)));

            if (csElement.IsFlag)
            {
                enumDecl = enumDecl.WithAttributeLists(SingletonList(
                    AttributeList(SingletonSeparatedList
                    (
                        Attribute(ParseName("System.FlagsAttribute"))
                    ))
                ));
            }

            yield return enumDecl;
        }
    }
}
