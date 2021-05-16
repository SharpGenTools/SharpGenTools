using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    public static class GeneratorHelpers
    {
        private static readonly PlatformDetectionType[] Platforms = (PlatformDetectionType[])Enum.GetValues(typeof(PlatformDetectionType));
        private static readonly PlatformDetectionType[] PlatformsNoAny = Platforms.Where(x => x != PlatformDetectionType.Any).ToArray();
        private static readonly int PlatformsNoAnyStringLength = PlatformsNoAny.Select(x => x.ToString().Length).Max() + 2;

        private static ThrowStatementSyntax ThrowException(TypeSyntax exception, string message) =>
            ThrowStatement(
                ObjectCreationExpression(
                    exception,
                    ArgumentList(
                        message == null
                            ? default
                            : SingletonSeparatedList(
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(message)))
                            )
                    ),
                    null
                )
            );

        private static readonly ThrowStatementSyntax ThrowPlatformNotSupportedStatement = ThrowException(
            ParseTypeName("System.PlatformNotSupportedException"), null
        );

        internal static readonly LiteralExpressionSyntax ZeroLiteral = LiteralExpression(
            SyntaxKind.NumericLiteralExpression, Literal(0)
        );

        public static ExpressionSyntax WrapInParentheses(ExpressionSyntax expression) =>
            expression is TypeSyntax or ParenthesizedExpressionSyntax or LiteralExpressionSyntax
                or InvocationExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax
                or MemberBindingExpressionSyntax or ThisExpressionSyntax or BaseExpressionSyntax
                ? expression
                : ParenthesizedExpression(expression);

        public static ExpressionSyntax LengthExpression(ExpressionSyntax expression) => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            WrapInParentheses(expression),
            IdentifierName("Length")
        );

        public static ExpressionSyntax OptionalLengthExpression(ExpressionSyntax expression) => BinaryExpression(
            SyntaxKind.CoalesceExpression,
            ConditionalAccessExpression(
                WrapInParentheses(expression),
                MemberBindingExpression(IdentifierName("Length"))
            ),
            ZeroLiteral
        );

        public static ExpressionSyntax CastExpression(TypeSyntax type, ExpressionSyntax expression)
        {
            var wrappedExpression = WrapInParentheses(expression);

            return expression is CastExpressionSyntax {Type: { } castType} && castType.IsEquivalentTo(type)
                ? wrappedExpression
                : SyntaxFactory.CastExpression(type, wrappedExpression);
        }

        public static ExpressionSyntax GenerateBoolToIntConversion(ExpressionSyntax valueExpression) =>
            ConditionalExpression(
                WrapInParentheses(valueExpression),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                ZeroLiteral
            );

        public static BinaryExpressionSyntax GenerateIntToBoolConversion(ExpressionSyntax valueExpression) =>
            BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                ZeroLiteral,
                valueExpression
            );

        public static TypeSyntax IntPtrType { get; } = ParseTypeName("System.IntPtr");

        public static TypeSyntax UIntPtrType { get; } = ParseTypeName("System.UIntPtr");

        public static MemberAccessExpressionSyntax IntPtrZero { get; } = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IntPtrType,
            IdentifierName(nameof(IntPtr.Zero))
        );

        public static TypeSyntax VoidPtrType { get; } = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));

        public static string GetPlatformSpecificSuffix(PlatformDetectionType platform)
        {
            if (platform == PlatformDetectionType.Any)
                return "_";

            StringBuilder str = new("_", PlatformsNoAnyStringLength);
            foreach (var flag in PlatformsNoAny)
            {
                if ((platform & flag) != flag)
                    continue;

                str.Append(flag);
                str.Append('_');
            }

            return str.ToString();
        }

        public static StatementSyntax GetPlatformSpecificStatements(GlobalNamespaceProvider globalNamespace,
                                                                    GeneratorConfig config,
                                                                    IEnumerable<PlatformDetectionType> types,
                                                                    Func<PlatformDetectionType, StatementSyntax> syntaxBuilder)
        {
            List<IfStatementSyntax> ifStatements = new();

            var allPlatformBitmap = config.Platforms;

            foreach (var platform in types)
            {
                var platformStatement = syntaxBuilder(platform);

                if ((platform & allPlatformBitmap) == allPlatformBitmap)
                    return platformStatement;

                ExpressionSyntax condition = null;

                foreach (var flag in PlatformsNoAny)
                {
                    if ((platform & flag) != flag)
                        continue;

                    var newCondition = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        globalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
                        IdentifierName("Is" + flag)
                    );

                    condition = condition is null
                                    ? newCondition
                                    : BinaryExpression(SyntaxKind.LogicalAndExpression, condition, newCondition);
                }

                ifStatements.Add(
                    condition is null
                        ? (IfStatementSyntax) platformStatement
                        : IfStatement(condition, platformStatement)
                );
            }

            IfStatementSyntax platformDetectionIfStatement = null;

            for (var i = ifStatements.Count - 1; i >= 0; i--)
            {
                platformDetectionIfStatement = ifStatements[i].WithElse(
                    platformDetectionIfStatement is null
                        ? ElseClause(ThrowPlatformNotSupportedStatement)
                        : ElseClause(platformDetectionIfStatement)
                );
            }

            return platformDetectionIfStatement;
        }
    }
}
