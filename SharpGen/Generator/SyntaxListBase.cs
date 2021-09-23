using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class SyntaxListBase<TValue, TSelf>
        : IReadOnlyList<TValue>, ICollection<TValue>, ICollection
        where TValue : CSharpSyntaxNode where TSelf : SyntaxListBase<TValue, TSelf>
    {
#nullable enable
        private readonly List<TValue> listImplementation = new(8);
        protected readonly Ioc? ioc;

        private MarshallingRegistry Registry =>
            ioc?.Generators.Marshalling ?? throw new Exception($"{nameof(MarshallingRegistry)} is required");

        protected GlobalNamespaceProvider GlobalNamespace =>
            ioc?.GlobalNamespace ?? throw new Exception($"{nameof(GlobalNamespaceProvider)} is required");

        protected GeneratorConfig Config =>
            ioc?.Generators.Config ?? throw new Exception($"{nameof(GeneratorConfig)} is required");

        protected SyntaxListBase(Ioc? ioc = null, IEnumerable<TValue?>? collection = null)
        {
            this.ioc = ioc;
            AddRange(collection);
        }

        protected abstract TSelf New { get; }

        protected TSelf From<T>(IEnumerable<T>? collection) where T : class
        {
            TSelf list = New;
            list.AddRange(collection);
            return list;
        }

        public void Add(TValue? item)
        {
            if (item == null)
                return;

            listImplementation.Add(item);
        }

        public void AddRange(IEnumerable<TValue?>? collection)
        {
            if (collection == null)
                return;

            listImplementation.AddRange(collection.Where(x => x != null)!);
        }

        public void Add<T>(T? item) where T : class => Add(Coerce(item));

        protected abstract TValue? Coerce<T>(T? value) where T : class;

        public void AddRange<T>(IEnumerable<T?>? collection) where T : class
        {
            if (collection == null)
                return;

            AddRange(collection.Select(Coerce));
        }

        protected abstract IEnumerable<TValue?> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types,
            Func<PlatformDetectionType, TResult> syntaxBuilder
        ) where TResult : class;

        protected abstract IEnumerable<TValue?> GetPlatformSpecificValue<TResult>(
            IEnumerable<PlatformDetectionType> types,
            Func<PlatformDetectionType, IEnumerable<TResult>> syntaxBuilder
        ) where TResult : class;

        protected bool TryAddGeneric<T, TSyntax, TGenerator>(T source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case ISingleCodeGenerator<T, TSyntax> codeGenerator:
                    Add(codeGenerator.GenerateCode(source));
                    return true;
                case IMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(codeGenerator.GenerateCode(source));
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAddGeneric<T, TSyntax, TGenerator>(IEnumerable<T> source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case ISingleCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(source, x => codeGenerator.GenerateCode(x));
                    return true;
                case IMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(source, x => codeGenerator.GenerateCode(x));
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAddPlatform<T, TSyntax, TGenerator>(T source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case IPlatformSingleCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(
                        GetPlatformSpecificValue(
                            codeGenerator.GetPlatforms(source),
                            platform => codeGenerator.GenerateCode(source, platform)
                        )
                    );
                    return true;
                case IPlatformMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(
                        GetPlatformSpecificValue(
                            codeGenerator.GetPlatforms(source),
                            platform => codeGenerator.GenerateCode(source, platform)
                        )
                    );
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAddPlatform<T, TSyntax, TGenerator>(IEnumerable<T> source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case IPlatformSingleCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(
                        source,
                        x => GetPlatformSpecificValue(
                            codeGenerator.GetPlatforms(x),
                            platform => codeGenerator.GenerateCode(x, platform)
                        )
                    );
                    return true;
                case IPlatformMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(
                        source,
                        x => GetPlatformSpecificValue(
                            codeGenerator.GetPlatforms(x),
                            platform => codeGenerator.GenerateCode(x, platform)
                        )
                    );
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAddPlatformFixed<T, TSyntax, TGenerator>(T source, PlatformDetectionType platform, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case IPlatformSingleCodeGenerator<T, TSyntax> codeGenerator:
                    Add(codeGenerator.GenerateCode(source, platform));
                    return true;
                case IPlatformMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(codeGenerator.GenerateCode(source, platform));
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAddPlatformFixed<T, TSyntax, TGenerator>(IEnumerable<T> source, PlatformDetectionType platform, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode
        {
            switch (generator)
            {
                case IPlatformSingleCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(source, x => codeGenerator.GenerateCode(x, platform));
                    return true;
                case IPlatformMultiCodeGenerator<T, TSyntax> codeGenerator:
                    AddRange(source, x => codeGenerator.GenerateCode(x, platform));
                    return true;
                default:
                    return false;
            }
        }

        protected bool TryAdd<T, TSyntax, TGenerator>(T source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode =>
            TryAddGeneric<T, TSyntax, TGenerator>(source, generator) || TryAddPlatform<T, TSyntax, TGenerator>(source, generator);

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        protected bool TryAdd<T, TSyntax, TGenerator>(IEnumerable<T> source, TGenerator generator)
            where T : CsBase where TSyntax : SyntaxNode =>
            TryAddGeneric<T, TSyntax, TGenerator>(source, generator) || TryAddPlatform<T, TSyntax, TGenerator>(source, generator);

#nullable restore

        public void AddRange<T, TResult>(T source, Func<IMarshaller, T, IEnumerable<TResult>> transform)
            where T : CsMarshalBase where TResult : class => AddRange(transform(Registry.GetMarshaller(source), source));

        public void AddRange<T, TResult>(IEnumerable<T> source, Func<IMarshaller, T, IEnumerable<TResult>> transform)
            where T : CsMarshalBase where TResult : class => AddRange(source.SelectMany(x => transform(Registry.GetMarshaller(x), x)));

        public void Add<T, TResult>(T source, Func<IMarshaller, T, TResult> transform)
            where T : CsMarshalBase where TResult : class => Add(transform(Registry.GetMarshaller(source), source));

        public void AddRange<T, TResult>(IEnumerable<T> source, Func<IMarshaller, T, TResult> transform)
            where T : CsMarshalBase where TResult : class => AddRange(source.Select(x => transform(Registry.GetMarshaller(x), x)));

        public void Add<T, TResult>(T source, Func<T, TResult> transform) where TResult : class => Add(transform(source));

        public void AddRange<T, TResult>(IEnumerable<T> source, Func<T, TResult> transform) where TResult : class =>
            AddRange(source.Select(transform));

        public void AddRange<T, TResult>(IEnumerable<T> source, Func<T, IEnumerable<TResult>> transform) where TResult : class =>
            AddRange(source.SelectMany(transform));

        public IEnumerator<TValue> GetEnumerator() => listImplementation.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => listImplementation.GetEnumerator();
        public void Clear() => listImplementation.Clear();
        public bool Contains(TValue item) => listImplementation.Contains(item);
        public void CopyTo(TValue[] array, int arrayIndex) => listImplementation.CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ((ICollection) listImplementation).CopyTo(array, index);
        public bool Remove(TValue item) => listImplementation.Remove(item);
        public int Count => listImplementation.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => listImplementation;
        public bool IsReadOnly => false;
        public int IndexOf(TValue item) => listImplementation.IndexOf(item);
        public void RemoveAt(int index) => listImplementation.RemoveAt(index);
        public TValue this[int index] => listImplementation[index];
    }
}