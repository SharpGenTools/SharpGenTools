using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ConstantCodeGenerator : IMultiCodeGenerator<CsVariable, FieldDeclarationSyntax>
    {
        private static SyntaxTrivia GenerateConstantDocumentationTrivia(CsVariable csVar)
        {
            return Trivia(DocumentationCommentTrivia(
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                List(
                    new XmlNodeSyntax[]{
                            XmlText(XmlTextNewLine("", true)),
                            XmlElement(
                                XmlElementStartTag(XmlName(Identifier("summary"))),
                                SingletonList<XmlNodeSyntax>(
                                    XmlText(TokenList(
                                        XmlTextLiteral($"Constant {csVar.Name}")))),
                                XmlElementEndTag(XmlName(Identifier("summary")))
                            ),
                            XmlText(XmlTextNewLine("\n", true)),
                            XmlElement(
                                XmlElementStartTag(XmlName(Identifier("unmanaged"))),
                                SingletonList<XmlNodeSyntax>(
                                    XmlText(TokenList(
                                        XmlTextLiteral(csVar.CppElementName)))),
                                XmlElementEndTag(XmlName(Identifier("unmanaged")))
                            ),
                            XmlText(XmlTextNewLine("\n", false))
                    })));
        }

        public IEnumerable<FieldDeclarationSyntax> GenerateCode(CsVariable var)
        {
            yield return FieldDeclaration(
                VariableDeclaration(
                    IdentifierName(var.TypeName),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(var.Name))
                        .WithInitializer(
                            EqualsValueClause(ParseExpression(var.Value))))))
                .WithModifiers(var.VisibilityTokenList)
                .WithLeadingTrivia(GenerateConstantDocumentationTrivia(var));
        }
    }
}
