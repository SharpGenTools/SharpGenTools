using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal static class GeneratorHelpers
    {
        private static readonly PlatformDetectionType[] Platforms = (PlatformDetectionType[])Enum.GetValues(typeof(PlatformDetectionType));
        private static readonly PlatformDetectionType[] PlatformsNoAny = Platforms.Where(x => x != PlatformDetectionType.Any).ToArray();
        private static readonly int PlatformsNoAnyStringLength = PlatformsNoAny.Select(x => x.ToString().Length).Max() + 2;

        private static readonly ThrowStatementSyntax ThrowPlatformNotSupportedStatement = ThrowStatement(
            ObjectCreationExpression(
                ParseTypeName("System.PlatformNotSupportedException")
            ).WithArgumentList(ArgumentList())
        );

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
