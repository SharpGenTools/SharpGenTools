using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class PropertyCodeGenerator : MemberCodeGeneratorBase<CsProperty>
    {
        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsProperty csElement)
        {
            var documentation = GenerateDocumentationTrivia(csElement).AddContent(
                XmlElement(
                    XmlElementStartTag(XmlName(Identifier("unmanaged"))),
                        SingletonList<XmlNodeSyntax>(
                            XmlText(TokenList(
                                XmlTextLiteral(csElement.CppElementName)))),
                        XmlElementEndTag(XmlName(Identifier("unmanaged")))
                    ),
                    XmlText(XmlTextNewLine("\n", false)));

            var accessors = new List<AccessorDeclarationSyntax>();

            if (csElement.IsPropertyParam)
            {
                if (csElement.Getter != null)
                {
                    if (csElement.IsPersistent)
                    {
                        accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(Block(
                                IfStatement(BinaryExpression(SyntaxKind.EqualsExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName($"{csElement.Name}__")),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    ExpressionStatement(
                                        InvocationExpression(IdentifierName(csElement.Getter.Name))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(IdentifierName($"{csElement.Name}__"))
                                                        .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))))),
                                ReturnStatement(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName($"{csElement.Name}__")))
                                )));
                    }
                    else
                    {
                        accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(Block(
                                ExpressionStatement(
                                    InvocationExpression(ParseExpression(csElement.Getter.Name))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    DeclarationExpression(
                                                        IdentifierName("var"),
                                                        SingleVariableDesignation(
                                                            Identifier("__output__"))))
                                                .WithRefOrOutKeyword(
                                                    Token(SyntaxKind.OutKeyword)))))),
                                ReturnStatement(IdentifierName("__output__")))));
                    }
                }
            }
            else
            {
                if (csElement.IsPersistent)
                {

                    accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                       .WithExpressionBody(ArrowExpressionClause(
                           BinaryExpression(SyntaxKind.CoalesceExpression,
                            IdentifierName($"{csElement.Name}__"),
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName($"{csElement.Name}__"),
                                InvocationExpression(ParseExpression(csElement.Getter.Name)))))));
                }
                else
                {
                    accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                       .WithExpressionBody(ArrowExpressionClause(
                           InvocationExpression(ParseExpression(csElement.Getter.Name)))));
                }
            }
            
            if (csElement.Setter != null)
            {
                var paramByRef = csElement.Setter.Parameters[0].ParamName.StartsWith("ref"); // TODO: Stop using param name and make this clean
                accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(ArrowExpressionClause(
                        InvocationExpression(ParseExpression(csElement.Getter.Name))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("value"))
                                            .WithRefOrOutKeyword(
                                                paramByRef ? Token(SyntaxKind.RefKeyword) : default)))))));
            }

            yield return PropertyDeclaration(ParseTypeName(csElement.PublicType.QualifiedName), Identifier(csElement.Name))
                .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)))
                .WithAccessorList(AccessorList(List(accessors)))
                .WithLeadingTrivia(Trivia(documentation));

            if (csElement.IsPersistent)
            {
                yield return FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName(csElement.PublicType.QualifiedName))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier($"{csElement.Name}__")))))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.ProtectedKeyword),
                                Token(SyntaxKind.InternalKeyword)}));
            }
        }
    }
}
