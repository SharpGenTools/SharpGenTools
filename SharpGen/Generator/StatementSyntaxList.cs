using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class StatementSyntaxList : SyntaxListBase<StatementSyntax, StatementSyntaxList>
    {
#nullable enable
        public StatementSyntaxList(Ioc? ioc = null) : base(ioc)
        {
        }

        protected override StatementSyntaxList New => new(ioc);

        protected override StatementSyntax? Coerce<T>(T? value) where T : class => value switch
        {
            null => null,
            StatementSyntax statement => statement,
            ExpressionSyntax expression => ExpressionStatement(expression),
            StatementSyntaxList statementList => statementList.ToStatement(),
            IEnumerable<StatementSyntax> statementList => From(statementList).ToStatement(),
            IEnumerable<ExpressionSyntax> statementList => From(statementList).ToStatement(),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        protected override IEnumerable<StatementSyntax> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types, Func<PlatformDetectionType, TResult> syntaxBuilder
        ) where TResult : class => new[]
        {
            GeneratorHelpers.GetPlatformSpecificStatements(
                GlobalNamespace, Config, types,
                platform => Coerce(syntaxBuilder(platform))
            )
        };

        protected override IEnumerable<StatementSyntax> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types, Func<PlatformDetectionType, IEnumerable<TResult>> syntaxBuilder
        ) where TResult : class => new[]
        {
            GeneratorHelpers.GetPlatformSpecificStatements(
                GlobalNamespace, Config, types,
                platform => Coerce(syntaxBuilder(platform))
            )
        };

#nullable restore

        public void Add<T>(T source, IStatementCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAdd<T, StatementSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            if (TryAdd<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void AddRange<T>(IEnumerable<T> source, IStatementCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAdd<T, StatementSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            if (TryAdd<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }

        public void Add<T>(T source, PlatformDetectionType platform, IStatementCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAddPlatformFixed<T, StatementSyntax, IStatementCodeGenerator<T>>(source, platform, generator))
                return;
            if (TryAddGeneric<T, StatementSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            if (TryAddPlatformFixed<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, platform, generator))
                return;
            if (TryAddGeneric<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void AddRange<T>(IEnumerable<T> source, PlatformDetectionType platform, IStatementCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAddPlatformFixed<T, StatementSyntax, IStatementCodeGenerator<T>>(source, platform, generator))
                return;
            if (TryAddGeneric<T, StatementSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            if (TryAddPlatformFixed<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, platform, generator))
                return;
            if (TryAddGeneric<T, ExpressionSyntax, IStatementCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }

        public BlockSyntax ToBlock()
        {
            var statement = ToStatement();
            return statement is BlockSyntax block ? block : Block(statement);
        }

        public StatementSyntax ToStatement() => Count == 1 ? this[0] : Block(this);

    }
}