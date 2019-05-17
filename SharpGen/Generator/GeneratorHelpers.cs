using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal static class GeneratorHelpers
    {
        private static readonly PlatformDetectionType[] platforms = (PlatformDetectionType[])Enum.GetValues(typeof(PlatformDetectionType));

        private static StatementSyntax PlatformSpecificStatement(GlobalNamespaceProvider globalNamespace, PlatformDetectionType allPlatformBitmap, PlatformDetectionType platform, StatementSyntax statement)
        {
            if ((platform & allPlatformBitmap) == allPlatformBitmap)
            {
                return statement;
            }

            ExpressionSyntax condition = null;

            foreach (PlatformDetectionType flag in platforms)
            {
                if ((platform & flag) == flag && flag != PlatformDetectionType.Any)
                {
                    var newCondition = MemberAccessExpression(
                                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                                globalNamespace.GetTypeNameSyntax(WellKnownName.PlatformDetection),
                                IdentifierName(flag.ToString()));
                    condition = condition is null ?
                        (ExpressionSyntax)newCondition
                        : BinaryExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalAndExpression,
                            condition,
                            newCondition);
                }
            }

            return condition is null ?
                statement
                : IfStatement(
                    condition,
                    statement);
        }

        public static string GetPlatformSpecificSuffix(PlatformDetectionType platform)
        {
            if (platform == PlatformDetectionType.Any)
            {
                return "_";
            }

            StringBuilder str = new StringBuilder("_");
            foreach (PlatformDetectionType flag in platforms)
            {
                if ((platform & flag) == flag && flag != PlatformDetectionType.Any)
                {
                    str.Append(flag);
                    str.Append('_');
                }
            }
            return str.ToString();
        }

        public static StatementSyntax GetPlatformSpecificStatements(GlobalNamespaceProvider globalNamespace, GeneratorConfig config, IEnumerable<PlatformDetectionType> types, Func<PlatformDetectionType, StatementSyntax> syntaxBuilder)
        {
            List<IfStatementSyntax> ifStatements = new List<IfStatementSyntax>();

            var allPlatformBitmap = config.Platforms;

            foreach (var platform in types)
            {
                if ((platform & allPlatformBitmap) == allPlatformBitmap)
                {
                    return PlatformSpecificStatement(globalNamespace, allPlatformBitmap, platform, syntaxBuilder(platform));
                }

                IfStatementSyntax statement = (IfStatementSyntax)PlatformSpecificStatement(globalNamespace, allPlatformBitmap, platform, syntaxBuilder(platform));
                ifStatements.Add(statement);
            }

            IfStatementSyntax platformDetectionIfStatement = null;

            for (int i = ifStatements.Count - 1; i >= 0; i--)
            {
                if (platformDetectionIfStatement is null)
                {
                    platformDetectionIfStatement = ifStatements[i]
                        .WithElse(
                        ElseClause(
                            ThrowStatement(
                                ObjectCreationExpression(ParseTypeName("System.PlatformNotSupportedException"))
                                    .WithArgumentList(ArgumentList()))));
                }
                else
                {
                    platformDetectionIfStatement = ifStatements[i].WithElse(ElseClause(platformDetectionIfStatement));
                }
            }

            return platformDetectionIfStatement;
        }
    }
}
