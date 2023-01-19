using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

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

    internal static readonly IdentifierNameSyntax VarIdentifierName = IdentifierName(
        Identifier(SyntaxTriviaList.Empty, SyntaxKind.VarKeyword, "var", "var", SyntaxTriviaList.Empty)
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

                var newCondition = PlatformCondition(globalNamespace, flag);

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

    public static ExpressionSyntax PlatformCondition(GlobalNamespaceProvider globalNamespace,
                                                     PlatformDetectionType flag) => MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        globalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
        IdentifierName("Is" + flag)
    );

    public static T WithLeadingIfDirective<T>(this T node, ExpressionSyntax condition) where T : SyntaxNode =>
        node.WithLeadingTrivia(
            node.GetLeadingTrivia().Insert(0, Trivia(IfDirectiveTrivia(condition, true, true, true)))
        );

    public static T WithTrailingEndIfDirective<T>(this T node) where T : SyntaxNode =>
        node.WithTrailingTrivia(node.GetTrailingTrivia().Add(Trivia(EndIfDirectiveTrivia(true))));

    public static T WithTrailingElseDirective<T>(this T node) where T : SyntaxNode =>
        node.WithTrailingTrivia(node.GetTrailingTrivia().Add(Trivia(ElseDirectiveTrivia(true, true))));

    public static LocalDeclarationStatementSyntax VarDeclaration(SyntaxToken name, ExpressionSyntax value) =>
        LocalDeclarationStatement(
            VariableDeclaration(
                VarIdentifierName,
                SingletonSeparatedList(VariableDeclarator(name, default, EqualsValueClause(value)))
            )
        );

    public static Func<CsMethod, int> GetOffsetGetter(PlatformDetectionType platform)
    {
        return platform switch
        {
            PlatformDetectionType.Windows => WindowsOffsetGetter,
            PlatformDetectionType.ItaniumSystemV => ItaniumSystemVOffsetGetter,
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

        static int WindowsOffsetGetter(CsMethod x) => x.WindowsOffset;
        static int ItaniumSystemVOffsetGetter(CsMethod x) => x.Offset;
    }

    public delegate ExpressionSyntax ExpressionTransform<in T>(T expression) where T : ExpressionSyntax;

    private static ExpressionSyntax IdentityExpressionTransformImpl<T>(T expression) where T : ExpressionSyntax =>
        expression;

    public static ExpressionSyntax PlatformSpecificExpression<T>(GlobalNamespaceProvider globalNamespace,
                                                                 PlatformDetectionType platform,
                                                                 Func<bool> anyPredicate,
                                                                 Func<T> windowsExpression,
                                                                 Func<T> nonWindowsExpression,
                                                                 ExpressionTransform<T> conditionalWrapper =
                                                                     null)
        where T : ExpressionSyntax
    {
        if ((platform & PlatformDetectionType.Any) == PlatformDetectionType.Any && anyPredicate())
        {
            conditionalWrapper ??= IdentityExpressionTransformImpl;
            return ConditionalExpression(
                PlatformCondition(globalNamespace, PlatformDetectionType.Windows),
                conditionalWrapper(windowsExpression()), conditionalWrapper(nonWindowsExpression())
            );
        }

        // Use the Windows offset for the default offset in the vtable when the Windows platform is requested for compat reasons.
        return (platform & PlatformDetectionType.Windows) != 0
                   ? windowsExpression()
                   : nonWindowsExpression();
    }

    public static readonly IdentifierNameSyntax PreprocessorNameSyntax = IdentifierName("NET6_0_OR_GREATER");
    public static readonly PrefixUnaryExpressionSyntax NotPreprocessorNameSyntax = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, PreprocessorNameSyntax);
}