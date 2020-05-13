using System;
using SharpGen.Logging;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SharpGen.Transform
{
    internal static class RelationParser
    {
        private const string StructSizeLegacy = "struct-size";
        private const string StructSizeReplacement = nameof(StructSizeRelation);
        private const string ConstLegacy = "const";
        private const string ConstReplacement = nameof(ConstantValueRelation);
        private const string WrapPrefix = "new[] { ";
        private const string WrapSuffix = " }";

        private static CSharpParseOptions _sharpParseOptions;

        internal static CSharpParseOptions SharpParseOptions =>
            _sharpParseOptions ??= new CSharpParseOptions(LanguageVersion.Preview);

        public static IList<MarshallableRelation> ParseRelation(string relation, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(relation))
                return null;

            var wrappedRelations = WrapRelation(
                relation,
                out var structSizeReplacementCount,
                out var constReplacementCount
            );

            try
            {
                var tree = SyntaxFactory.ParseExpression(wrappedRelations, options: SharpParseOptions);

                if (!(tree is ImplicitArrayCreationExpressionSyntax arrayCreationTree))
                {
                    logger.Error(LoggingCodes.InvalidRelation, "Relation [{0}] parse failed: internal expression wrapping error. Ignoring.", relation);
                    return null;
                }

                var relationsList = arrayCreationTree.Initializer.Expressions;
                var result = relationsList.Select(ParseSingleRelation).ToArray();

                if (result.All(x => x.Relation != null))
                {
                    // Success, do diagnostics and return result.

                    if (structSizeReplacementCount != result.Count(x => x.Relation is StructSizeRelation && x.Identifier == StructSizeReplacement))
                        logger.Warning(
                            LoggingCodes.InvalidRelation,
                            "Relation [{0}] replaced extra \"{1}\" substrings.", relation, StructSizeLegacy
                        );

                    if (constReplacementCount != result.Count(x => x.Relation is ConstantValueRelation && x.Identifier == ConstReplacement))
                        logger.Warning(
                            LoggingCodes.InvalidRelation,
                            "Relation [{0}] replaced extra \"{1}\" substrings.", relation, ConstLegacy
                        );

                    return result.Select(x => x.Relation).ToArray();
                }
                
                // At least one relation failed to parse.

                foreach (var (failedResult, i) in result.Select((x, i) => (Result: x, i)).Where(x => x.Result.Relation == null))
                {
                    // If source of the failure is too short, it wouldn't look good or helpful.
                    // Use full span of that failed relation instead.
                    var sourceSpan = failedResult.IssueSource.Length > 3
                        ? failedResult.IssueSource
                        : relationsList[i].Span;

                    logger.Error(
                        LoggingCodes.InvalidRelation,
                        "Sub-relation [{0}] parse failed: {1}.",
                        wrappedRelations.Substring(sourceSpan.Start, sourceSpan.Length),
                        failedResult.IssueDescription
                    );
                }

                logger.Error(LoggingCodes.InvalidRelation, "Relation [{0}] parse failed due to the previous errors. Ignoring.", relation);
                return null;
            }
            catch (Exception exception)
            {
                logger.Error(
                    LoggingCodes.InvalidRelation,
                    "Relation [{0}] parse failed: {1} — {2}. Ignoring.",
                    exception,
                    relation,
                    exception.GetType().Name,
                    exception.Message
                );

                return null;
            }
        }

        private static string WrapRelation(string relation, out uint structSizeReplacementCount, out uint constReplacementCount)
        {
            var wrappedRelationBuilder = new StringBuilder(WrapPrefix, WrapPrefix.Length + relation.Length + WrapSuffix.Length);

            structSizeReplacementCount = 0;
            constReplacementCount = 0;
            var offset = 0;

            while (true)
            {
                var structSizeIndex = relation.IndexOf(StructSizeLegacy, offset, StringComparison.Ordinal);
                var constIndex = relation.IndexOf(ConstLegacy, offset, StringComparison.Ordinal);
                var indexes = new[] {structSizeIndex, constIndex}.Where(x => x != -1).ToArray();

                if (indexes.Length == 0)
                {
                    wrappedRelationBuilder.Append(relation, offset, relation.Length - offset);
                    break;
                }

                var index = indexes.Min();

                wrappedRelationBuilder.Append(relation, offset, index - offset);

                if (index == structSizeIndex)
                {
                    ++structSizeReplacementCount;
                    wrappedRelationBuilder.Append(StructSizeReplacement);
                    offset = index + StructSizeLegacy.Length;
                }
                else if (index == constIndex)
                {
                    ++constReplacementCount;
                    wrappedRelationBuilder.Append(ConstReplacement);
                    offset = index + ConstLegacy.Length;
                }
                else
                {
                    throw new Exception($"Internal {nameof(RelationParser)} indexes error");
                }
            }

            wrappedRelationBuilder.Append(WrapSuffix);

            return wrappedRelationBuilder.ToString();
        }

        private static ParseResult ParseSingleRelation(ExpressionSyntax item)
        {
            if (!(item is InvocationExpressionSyntax invocationExpression))
                return new ParseResult(ParseResult.ParseIssue.RelationInvocationExpected, item.Span);

            var functionNameExpression = invocationExpression.Expression;

            if (!(functionNameExpression is IdentifierNameSyntax functionIdentifier))
                return new ParseResult(
                    ParseResult.ParseIssue.SimpleIdentifierAsFunctionNameExpected,
                    functionNameExpression.Span
                );

            var argumentList = invocationExpression.ArgumentList.Arguments;

            ParseResult ParseLengthRelation(string identifier)
            {
                return argumentList.Count switch
                {
                    1 => new ParseResult(
                        new LengthRelation
                        {
                            Identifier = argumentList[0].Expression.ToString()
                        },
                        identifier
                    ),
                    _ => new ParseResult(ParseResult.ParseIssue.ArgumentCountMismatch, argumentList.Span)
                };
            }

            ParseResult ParseConstantValueRelation(string identifier)
            {
                return argumentList.Count switch
                {
                    1 => new ParseResult(
                        new ConstantValueRelation
                        {
                            Value = argumentList[0].Expression
                        },
                        identifier
                    ),
                    _ => new ParseResult(ParseResult.ParseIssue.ArgumentCountMismatch, argumentList.Span)
                };
            }

            ParseResult ParseStructSizeRelation(string identifier)
            {
                return argumentList.Count switch
                {
                    0 => new ParseResult(new StructSizeRelation(), identifier),
                    _ => new ParseResult(ParseResult.ParseIssue.ArgumentCountMismatch, argumentList.Span)
                };
            }

            var identifierText = functionIdentifier.Identifier.ValueText;
            return identifierText switch
            {
                "length" => ParseLengthRelation(identifierText),
                "Length" => ParseLengthRelation(identifierText),
                ConstReplacement => ParseConstantValueRelation(identifierText),
                "Const" => ParseConstantValueRelation(identifierText),
                StructSizeReplacement => ParseStructSizeRelation(identifierText),
                "StructSize" => ParseStructSizeRelation(identifierText),
                _ => new ParseResult(ParseResult.ParseIssue.UnknownRelation, functionIdentifier.Span)
            };
        }

        private sealed class ParseResult
        {
            public ParseResult(MarshallableRelation relation, string identifier)
            {
                Relation = relation ?? throw new ArgumentNullException(nameof(relation));
                Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            }

            public ParseResult(ParseIssue issue, TextSpan issueSource)
            {
                Issue = issue;
                IssueSource = issueSource;
            }

            public MarshallableRelation Relation { get; }
            public string Identifier { get; }
            public ParseIssue Issue { get; }
            public TextSpan IssueSource { get; }

            public enum ParseIssue
            {
                ArgumentCountMismatch,
                UnknownRelation,
                RelationInvocationExpected,
                SimpleIdentifierAsFunctionNameExpected
            }

            public string IssueDescription => Issue switch
            {
                ParseIssue.ArgumentCountMismatch => "mismatched argument count",
                ParseIssue.UnknownRelation => "unknown relation name",
                ParseIssue.RelationInvocationExpected => "expected relation invocation",
                ParseIssue.SimpleIdentifierAsFunctionNameExpected => "expected simple identifier as relation name",
                _ => throw new Exception($"Unknown {nameof(ParseIssue)}")
            };
        }
    }
}