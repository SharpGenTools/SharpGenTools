using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class PropertyCodeGenerator : MemberCodeGeneratorBase<CsProperty>
    {
        public PropertyCodeGenerator(IGeneratorRegistry generators, IDocumentationAggregator documentation)
            :base(documentation)
        {
            Generators = generators;
        }

        private IGeneratorRegistry Generators { get; }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsProperty csElement)
        {
            var documentation = GenerateDocumentationTrivia(csElement);

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
                if (csElement.Getter != null)
                {
                    if (csElement.IsPersistent)
                    {

                        accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                           .WithExpressionBody(ArrowExpressionClause(
                               BinaryExpression(SyntaxKind.CoalesceExpression,
                                IdentifierName($"{csElement.Name}__"),
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName($"{csElement.Name}__"),
                                    InvocationExpression(ParseExpression(csElement.Getter.Name))))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    }
                    else
                    {
                        accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                           .WithExpressionBody(ArrowExpressionClause(
                               InvocationExpression(ParseExpression(csElement.Getter.Name))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    } 
                }
            }
            
            if (csElement.Setter != null)
            {
                var paramByRef = Generators.Parameter.GenerateCode(csElement.Setter.Parameters[0])
                    .Modifiers.Select(token => token.Kind()).Contains(SyntaxKind.RefKeyword);

                accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(ArrowExpressionClause(
                        InvocationExpression(ParseExpression(csElement.Setter.Name))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("value"))
                                            .WithRefOrOutKeyword(
                                                paramByRef ? Token(SyntaxKind.RefKeyword) : default))))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            yield return PropertyDeclaration(
                ParseTypeName(csElement.PublicType.QualifiedName),
                Identifier(csElement.Name))
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
