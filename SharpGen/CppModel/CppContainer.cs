using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SharpGen.CppModel
{
    public abstract class CppContainer : CppElement
    {
        private List<CppElement> items;

        public IReadOnlyList<CppElement> Items
        {
            get => (IReadOnlyList<CppElement>) items ?? ImmutableList<CppElement>.Empty;
            set => AdoptAllChildren(
                items = value switch
                {
                    List<CppElement> list => list,
                    _ => new List<CppElement>(value)
                }
            );
        }

        protected internal virtual IEnumerable<CppElement> AllItems => Iterate<CppElement>();

        public bool IsEmpty => items == null || items.Count == 0;

        protected CppContainer(string name) : base(name)
        {
        }

        public void Add(CppElement element)
        {
            AdoptChild(element);
            items ??= new List<CppElement>();

            items.Add(element);
        }

        public void AddRange(IEnumerable<CppElement> elements)
        {
            items ??= new List<CppElement>();

            var index = items.Count;
            items.AddRange(elements);

            var newCount = items.Count;
            for (var i = index; i < newCount; i++)
                AdoptChild(items[i]);
        }

        private void AdoptChild(CppElement element)
        {
            element.Parent?.items?.Remove(element);
            element.Parent = this;
        }

        private void AdoptAllChildren(IEnumerable<CppElement> elements)
        {
            foreach (var element in elements)
                AdoptChild(element);
        }

        internal void RemoveChild(CppElement child) => items?.Remove(child);

        /// <summary>
        ///   Iterates on items on this instance.
        /// </summary>
        /// <typeparam name = "T">Type of the item to iterate</typeparam>
        /// <returns>An enumeration on items</returns>
        public IEnumerable<T> Iterate<T>() where T : CppElement =>
            items == null ? Enumerable.Empty<T>() : Items.OfType<T>();
    }
}