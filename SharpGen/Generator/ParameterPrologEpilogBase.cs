using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ParameterPrologEpilogBase
    {

        protected static StatementSyntax GenerateNullCheckIfNeeded(CsParameter param, bool checkStructClass, StatementSyntax statement)
        {
            if (param.IsOptional && (!checkStructClass || param.IsStructClass))
            {
                return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(param.Name),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                statement);
            }
            return statement;
        }

        protected ExpressionSyntax GenerateNullCheckIfNeeded(CsParameter param, bool checkStructClass, ExpressionSyntax expression, ExpressionSyntax nullAlternative)
        {
            if (param.IsOptional && (!checkStructClass || param.IsStructClass))
            {
                return ConditionalExpression(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        IdentifierName(param.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        nullAlternative,
                        expression);
            }
            return expression;
        }

        protected StatementSyntax LoopThroughArrayParameter(string parameterName, StatementSyntax loopBody, string variableName)
        {
            return ForStatement(loopBody)
                .WithDeclaration(
                    VariableDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(variableName))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0)))))))
                .WithCondition(
                    BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        IdentifierName(variableName),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(parameterName),
                            IdentifierName("Length"))))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            IdentifierName(variableName))));
        }

    }
}
