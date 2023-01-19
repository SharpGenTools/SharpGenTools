using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class PropertyCodeGenerator : MemberMultiCodeGeneratorBase<CsProperty>
{
    public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsProperty csElement)
    {
        var accessors = new List<AccessorDeclarationSyntax>();
        var elementType = ParseTypeName(csElement.PublicType.QualifiedName);
        var isPersistent = csElement.IsPersistent;

        SyntaxToken fieldName;
        IdentifierNameSyntax fieldIdentifier;
        if (isPersistent)
        {
            fieldName = csElement.PersistentFieldIdentifier;
            fieldIdentifier = IdentifierName(fieldName);
        }
        else
        {
            fieldName = default;
            fieldIdentifier = null;
        }

        if (csElement.Getter != null)
        {
            var getterName = IdentifierName(csElement.Getter.Name);
            if (csElement.IsPropertyParam)
            {
                StatementSyntaxList body = new();
                if (isPersistent)
                {
                    if (csElement.IsValueType)
                    {
                        body.Add(
                            IfStatement(
                                BinaryExpression(SyntaxKind.EqualsExpression,
                                                 fieldIdentifier,
                                                 NullLiteral),
                                Block(
                                    LocalDeclarationStatement(
                                        VariableDeclaration(elementType)
                                           .WithVariables(
                                                SingletonSeparatedList(
                                                    VariableDeclarator(Identifier("temp"))))),
                                    ExpressionStatement(
                                        InvocationExpression(getterName)
                                           .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(IdentifierName("temp"))
                                                           .WithRefOrOutKeyword(
                                                                Token(SyntaxKind.OutKeyword)))))),
                                    ExpressionStatement(
                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                             fieldIdentifier,
                                                             IdentifierName("temp")))))
                        );
                        body.Add(ReturnStatement(
                                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                ThisExpression(),
                                                                fieldIdentifier),
                                                            IdentifierName("Value"))));
                    }
                    else
                    {
                        body.Add(
                            IfStatement(BinaryExpression(SyntaxKind.EqualsExpression,
                                                         MemberAccessExpression(
                                                             SyntaxKind.SimpleMemberAccessExpression,
                                                             ThisExpression(),
                                                             fieldIdentifier),
                                                         NullLiteral),
                                        ExpressionStatement(
                                            InvocationExpression(getterName)
                                               .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(fieldIdentifier)
                                                               .WithRefOrOutKeyword(
                                                                    Token(SyntaxKind.OutKeyword))))))));
                        body.Add(ReturnStatement(
                                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            ThisExpression(),
                                                            fieldIdentifier)));
                    }
                }
                else
                {
                    var output = Identifier("__output__");
                    body.Add(
                        ExpressionStatement(
                            InvocationExpression(
                                getterName,
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                                DeclarationExpression(
                                                    GeneratorHelpers.VarIdentifierName,
                                                    SingleVariableDesignation(output)))
                                           .WithRefOrOutKeyword(
                                                Token(SyntaxKind.OutKeyword))))
                            )
                        )
                    );
                    body.Add(ReturnStatement(IdentifierName(output)));
                }

                accessors.Add(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(body.ToBlock())
                );
            }
            else
            {
                ExpressionSyntax initializer = isPersistent
                                                   ? AssignmentExpression(
                                                       SyntaxKind.CoalesceAssignmentExpression,
                                                       fieldIdentifier,
                                                       InvocationExpression(getterName)
                                                   )
                                                   : InvocationExpression(getterName);

                accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                             .WithExpressionBody(ArrowExpressionClause(initializer))
                             .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }
        }

        if (csElement.Setter != null)
        {
            var paramByRef = GetMarshaller(csElement.Setter.Parameters[0])
                            .GenerateManagedArgument(csElement.Setter.Parameters[0])
                            .RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword);

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

        yield return AddDocumentationTrivia(
            PropertyDeclaration(elementType, Identifier(csElement.Name))
               .WithModifiers(csElement.VisibilityTokenList)
               .WithAccessorList(AccessorList(List(accessors))),
            csElement
        );

        if (isPersistent)
        {
            yield return FieldDeclaration(
                    VariableDeclaration(
                            csElement.IsValueType
                                ? NullableType(elementType)
                                : elementType
                        )
                       .WithVariables(SingletonSeparatedList(VariableDeclarator(fieldName))))
               .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)));
        }
    }

    public PropertyCodeGenerator(Ioc ioc) : base(ioc)
    {
    }
}