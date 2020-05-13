using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class RelationParserTests : TestBase
    {
        public RelationParserTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void ParseFailsOnMissingInput()
        {
            using (LoggerEmptyEnvironment())
            {
                Assert.Null(RelationParser.ParseRelation(null, Logger));
            }
            
            using (LoggerEmptyEnvironment())
            {
                Assert.Null(RelationParser.ParseRelation("", Logger));
            }
            
            using (LoggerEmptyEnvironment())
            {
                Assert.Null(RelationParser.ParseRelation("        ", Logger));
            }
        }

        [Fact]
        public void ParseFailsOnUnexpectedInput()
        {
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                Assert.Null(RelationParser.ParseRelation("Samaritan", Logger));
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                Assert.Null(RelationParser.ParseRelation("struct(", Logger));
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                Assert.Null(RelationParser.ParseRelation("struct()", Logger));
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                Assert.Null(RelationParser.ParseRelation("struct-size(42)", Logger));
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            {
                Assert.Null(RelationParser.ParseRelation("length()", Logger));
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Error))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Error))
            using (LoggerCodeRequiredEnvironment(LoggingCodes.InvalidRelation))
            {
                Assert.Null(RelationParser.ParseRelation("Math.Max(42)", Logger));
            }
        }

        [Fact]
        public void ParseMixedRelations()
        {
            using (LoggerEmptyEnvironment())
            {
                const string const1 = "int.MaxValue";
                const string const2 = "Math.Max( int.MinValue,Math.Pow(10, 3) ) ";
                
                var relations = RelationParser.ParseRelation($"length(initialValue), const({const1}), const({const2})", Logger);
                Assert.NotNull(relations);
                Assert.Equal(3, relations.Count);
                
                Assert.IsType<LengthRelation>(relations[0]);
                Assert.Equal("initialValue", ((LengthRelation) relations[0]).Identifier);
                
                Assert.IsType<ConstantValueRelation>(relations[1]);
                Assert.IsType<MemberAccessExpressionSyntax>(((ConstantValueRelation) relations[1]).Value);
                Assert.Equal(const1, ((ConstantValueRelation) relations[1]).Value.ToFullString());
                
                Assert.IsType<ConstantValueRelation>(relations[2]);
                Assert.IsType<InvocationExpressionSyntax>(((ConstantValueRelation) relations[2]).Value);
                Assert.Equal(const2, ((ConstantValueRelation) relations[2]).Value.ToFullString());
            }
        }

        [Fact]
        public void ParseMultipleLengths()
        {
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("length(initialValue),length(initialVelocity)", Logger);
                Assert.NotNull(relations);
                Assert.Equal(2, relations.Count);
                Assert.All(relations, relation => Assert.IsType<LengthRelation>(relation));
                Assert.Equal("initialValue", ((LengthRelation) relations[0]).Identifier);
                Assert.Equal("initialVelocity", ((LengthRelation) relations[1]).Identifier);
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation(" length ( initialValue )  , length ( initialVelocity ) ", Logger);
                Assert.NotNull(relations);
                Assert.Equal(2, relations.Count);
                Assert.All(relations, relation => Assert.IsType<LengthRelation>(relation));
                Assert.Equal("initialValue", ((LengthRelation) relations[0]).Identifier);
                Assert.Equal("initialVelocity", ((LengthRelation) relations[1]).Identifier);
            }
        }

        [Fact]
        public void ParsePreservesContents()
        {
            using (LoggerEmptyEnvironment())
            {
                const string value = "Math.Max( int.MinValue, Math.Pow(10, 3) )";
                
                var relations = RelationParser.ParseRelation($"const ( {value} )", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<InvocationExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "Math.Max( int.MinValue, Math.Pow (10, 3) )";
                
                var relations = RelationParser.ParseRelation($"const ( {value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<InvocationExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "\"Real Control is surgical. Invisible.\"";
                
                var relations = RelationParser.ParseRelation($"const ({value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "\"Real Control is surgical (invisible ).\"";
                
                var relations = RelationParser.ParseRelation($"const ({value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "\"Real Control is surgical ( invisible .\"";
                var relations = RelationParser.ParseRelation($"const ({value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "\"Real Control is surgical. Invisible)).\"";
                var relations = RelationParser.ParseRelation($"const ({value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "DEBUG";
                var relations = RelationParser.ParseRelation($"const({value})", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<IdentifierNameSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
            
            using (LoggerEmptyEnvironment())
            {
                const string value = "((( ())) )";
                var relations = RelationParser.ParseRelation($"const ({value} )", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<ParenthesizedExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(value, ((ConstantValueRelation) relations[0]).Value.ToString());
            }
        }

        [Fact]
        public void ParseSimpleConstantValue()
        {
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("Const(null)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(SyntaxKind.NullLiteralExpression, ((LiteralExpressionSyntax)((ConstantValueRelation) relations[0]).Value).Kind());
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("Const(default)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal(SyntaxKind.DefaultLiteralExpression, ((LiteralExpressionSyntax)((ConstantValueRelation) relations[0]).Value).Kind());
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("const(0)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal("0", ((ConstantValueRelation) relations[0]).Value.ToFullString());
            }
        }

        [Fact]
        public void ParseSimpleLength()
        {
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("length(42)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<LengthRelation>(relations[0]);
                Assert.Equal("42", ((LengthRelation) relations[0]).Identifier);
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("Length(42)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<LengthRelation>(relations[0]);
                Assert.Equal("42", ((LengthRelation) relations[0]).Identifier);
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("length(-abc/123)", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<LengthRelation>(relations[0]);
                Assert.Equal("-abc/123", ((LengthRelation) relations[0]).Identifier);
            }
        }

        [Fact]
        public void ParseSimpleStructSize()
        {
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("StructSize()", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<StructSizeRelation>(relations[0]);
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("struct-size()", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<StructSizeRelation>(relations[0]);
            }
            
            using (LoggerEmptyEnvironment())
            {
                var relations = RelationParser.ParseRelation("struct-size(     )", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<StructSizeRelation>(relations[0]);
            }
        }

        [Fact]
        public void ParseWarnsOnExtraReplacedSubstrings()
        {
            using (LoggerMessageCountEnvironment(1, LogLevel.Warning))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Warning))
            {
                var relations = RelationParser.ParseRelation("const(\"struct-size\")", Logger);
                Assert.NotNull(relations);
                Assert.Single(relations);
                Assert.IsType<ConstantValueRelation>(relations[0]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[0]).Value);
                Assert.Equal($"\"{nameof(StructSizeRelation)}\"", ((ConstantValueRelation) relations[0]).Value.ToFullString());
            }
            
            using (LoggerMessageCountEnvironment(2, LogLevel.Warning))
            using (LoggerMessageCountEnvironment(0, ~LogLevel.Warning))
            {
                var relations = RelationParser.ParseRelation("struct-size(), const(\"struct-size\"), const(\"const(42)\")", Logger);
                Assert.NotNull(relations);
                Assert.Equal(3, relations.Count);
                Assert.IsType<StructSizeRelation>(relations[0]);
                
                Assert.IsType<ConstantValueRelation>(relations[1]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[1]).Value);
                Assert.Equal($"\"{nameof(StructSizeRelation)}\"", ((ConstantValueRelation) relations[1]).Value.ToFullString());
                
                Assert.IsType<ConstantValueRelation>(relations[2]);
                Assert.IsType<LiteralExpressionSyntax>(((ConstantValueRelation) relations[2]).Value);
                Assert.Equal($"\"{nameof(ConstantValueRelation)}(42)\"", ((ConstantValueRelation) relations[2]).Value.ToFullString());
            }
        }
    }
}