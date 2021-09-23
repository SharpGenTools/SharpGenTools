using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal sealed class MemberSyntaxList : SyntaxListBase<MemberDeclarationSyntax, MemberSyntaxList>
    {
#nullable enable
        public MemberSyntaxList(Ioc? ioc = null) : base(ioc)
        {
        }

        protected override MemberSyntaxList New => new(ioc);

        protected override MemberDeclarationSyntax? Coerce<T>(T? value) where T : class => value switch
        {
            null => null,
            MemberDeclarationSyntax member => member,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        protected override IEnumerable<MemberDeclarationSyntax?> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types, Func<PlatformDetectionType, TResult> syntaxBuilder
        ) => types.Where(sig => (sig & Config.Platforms) != 0).Select(sig => Coerce(syntaxBuilder(sig)));

        protected override IEnumerable<MemberDeclarationSyntax?> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types,
            Func<PlatformDetectionType, IEnumerable<TResult>> syntaxBuilder
        ) => types.Where(sig => (sig & Config.Platforms) != 0).SelectMany(syntaxBuilder).Select(Coerce);

        public void Add<T>(T source, IMemberCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAdd<T, MemberDeclarationSyntax, IMemberCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void AddRange<T>(IEnumerable<T> source, IMemberCodeGenerator<T> generator) where T : CsBase
        {
            if (TryAdd<T, MemberDeclarationSyntax, IMemberCodeGenerator<T>>(source, generator))
                return;
            throw new ArgumentOutOfRangeException(nameof(generator));
        }
#nullable restore
    }
}