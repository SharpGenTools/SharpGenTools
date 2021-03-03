using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SharpGen.Doc;

namespace SharpGen.Platform.Documentation
{
    /// <inheritdoc />
    internal sealed class DocSubItem : IDocSubItem
    {
        private readonly ObservableSet<string> attributes =
            new(StringComparer.InvariantCultureIgnoreCase);

        private string description;
        private string term;

        public DocSubItem()
        {
            attributes.CollectionChanged += OnCollectionChanged;
        }

        public string Term
        {
            get => term;
            set
            {
                if (term == value) return;

                term = value;
                IsDirty = true;
            }
        }

        public string Description
        {
            get => description;
            set
            {
                if (description == value) return;

                description = value;
                IsDirty = true;
            }
        }

        public IList<string> Attributes => attributes;

        public bool IsDirty { get; set; }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
        }
    }
}