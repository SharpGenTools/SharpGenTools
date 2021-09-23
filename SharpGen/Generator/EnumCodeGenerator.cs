using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Logging;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class EnumCodeGenerator : MemberSingleCodeGeneratorBase<CsEnum>
    {
        private static readonly SyntaxList<AttributeListSyntax> FlagAttributeList = SingletonList(
            AttributeList(SingletonSeparatedList(Attribute(ParseName("System.FlagsAttribute"))))
        );

        public override MemberDeclarationSyntax GenerateCode(CsEnum csElement)
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
                                                                 GetValueLiteral(item.Value, csElement.Name, item.Name)
                                                             )))));
                                         }

                                         return AddDocumentationTrivia(itemDecl, item);
                                     })
                                    .ToArray()
                       );

            if (csElement.IsFlag)
                enumDecl = enumDecl.WithAttributeLists(FlagAttributeList);

            return AddDocumentationTrivia(enumDecl, csElement);
        }

        private SyntaxToken GetValueLiteral(string value, string enumName, string enumItemName)
        {
            // [...] integer types in rank order, signed given preference over unsigned.
            if (int.TryParse(value, out var valueInt))
                return Literal(valueInt);
            if (uint.TryParse(value, out var valueUInt))
                return Literal(valueUInt);
            if (long.TryParse(value, out var valueLong))
                return Literal(valueLong);
            if (ulong.TryParse(value, out var valueULong))
                return Literal(valueULong);

            Logger.Error(
                LoggingCodes.EnumItemLiteralOutOfRange,
                "Enum [{0}] value [{1}]=[{2}] is out of range of any valid numeric type on .NET platform",
                enumName, enumItemName, value
            );

            return default;
        }

        public EnumCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
