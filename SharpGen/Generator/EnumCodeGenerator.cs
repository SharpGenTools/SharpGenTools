using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class EnumCodeGenerator : MemberCodeGeneratorBase<CsEnum>
    {
        private static readonly SyntaxList<AttributeListSyntax> FlagAttributeList = SingletonList(
            AttributeList(SingletonSeparatedList(Attribute(ParseName("System.FlagsAttribute"))))
        );

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsEnum csElement)
        {
            var enumDecl = EnumDeclaration(csElement.Name);
            var underlyingType = ParseTypeName(csElement.UnderlyingType.Name);
            enumDecl = enumDecl
                      .WithModifiers(csElement.VisibilityTokenList)
                      .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(underlyingType))))
                      .AddMembers(
                           csElement.EnumItems
                                    .Select(item =>
                                     {
                                         var itemDecl = EnumMemberDeclaration(item.Name);

                                         if (!string.IsNullOrEmpty(item.Value))
                                         {
                                             itemDecl = itemDecl.WithEqualsValue(
                                                 EqualsValueClause(
                                                     CheckedExpression(
                                                         SyntaxKind.UncheckedExpression,
                                                         CastExpression(
                                                             underlyingType,
                                                             LiteralExpression(
                                                                 SyntaxKind.NumericLiteralExpression,
                                                                 Literal(int.Parse(item.Value))
                                                             )))));
                                         }

                                         return AddDocumentationTrivia(itemDecl, item);
                                     })
                                    .ToArray()
                       );

            if (csElement.IsFlag)
                enumDecl = enumDecl.WithAttributeLists(FlagAttributeList);

            yield return AddDocumentationTrivia(enumDecl, csElement);
        }

        public EnumCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
