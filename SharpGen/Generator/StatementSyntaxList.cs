using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class StatementSyntaxList : IReadOnlyList<StatementSyntax>, ICollection<StatementSyntax>,
                                                ICollection
    {
        private readonly List<StatementSyntax> listImplementation = new(8);
        private readonly MarshallingRegistry registry;

        private MarshallingRegistry Registry =>
            registry ?? throw new Exception($"{nameof(MarshallingRegistry)} is required");

        public StatementSyntaxList(MarshallingRegistry registry = null) => this.registry = registry;

        public void Add(StatementSyntax item)
        {
            if (item == null)
                return;

            listImplementation.Add(item);
        }

        public void AddRange(IEnumerable<StatementSyntax> collection)
        {
            if (collection == null)
                return;

            listImplementation.AddRange(collection.Where(x => x != null));
        }

        public void AddRange<T>(T source, Func<IMarshaller, T, IEnumerable<StatementSyntax>> transform)
            where T : CsMarshalBase => AddRange(transform(Registry.GetMarshaller(source), source));

        public void AddRange<T>(IEnumerable<T> source, Func<IMarshaller, T, IEnumerable<StatementSyntax>> transform)
            where T : CsMarshalBase => AddRange(source.SelectMany(x => transform(Registry.GetMarshaller(x), x)));

        public void Add<T>(T source, Func<IMarshaller, T, StatementSyntax> transform)
            where T : CsMarshalBase => Add(transform(Registry.GetMarshaller(source), source));

        public void AddRange<T>(IEnumerable<T> source, Func<IMarshaller, T, StatementSyntax> transform)
            where T : CsMarshalBase => AddRange(source.Select(x => transform(Registry.GetMarshaller(x), x)));

        public void Add<T>(T source, Func<T, StatementSyntax> transform) => Add(transform(source));

        public void AddRange<T>(IEnumerable<T> source, Func<T, StatementSyntax> transform) =>
            AddRange(source.Select(transform));

        public BlockSyntax ToBlock()
        {
            var statement = ToStatement();
            return statement is BlockSyntax block ? block : Block(statement);
        }

        public StatementSyntax ToStatement() => Count == 1 ? this[0] : Block(this);

        public IEnumerator<StatementSyntax> GetEnumerator() => listImplementation.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => listImplementation.GetEnumerator();
        public void Clear() => listImplementation.Clear();
        public bool Contains(StatementSyntax item) => listImplementation.Contains(item);
        public void CopyTo(StatementSyntax[] array, int arrayIndex) => listImplementation.CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ((ICollection) listImplementation).CopyTo(array, index);
        public bool Remove(StatementSyntax item) => listImplementation.Remove(item);
        public int Count => listImplementation.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => listImplementation;
        public bool IsReadOnly => false;
        public int IndexOf(StatementSyntax item) => listImplementation.IndexOf(item);
        public void RemoveAt(int index) => listImplementation.RemoveAt(index);
        public StatementSyntax this[int index] => listImplementation[index];
    }
}