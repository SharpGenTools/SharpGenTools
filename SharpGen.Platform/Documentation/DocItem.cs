using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SharpGen.Doc;

namespace SharpGen.Platform.Documentation;

internal sealed class DocItem : IDocItem
{
    private readonly ObservableCollection<IDocSubItem> items = new();

    private readonly ObservableSet<string> names =
        new(StringComparer.InvariantCultureIgnoreCase);

    private readonly ObservableSet<string> seeAlso =
        new(StringComparer.InvariantCultureIgnoreCase);

    private bool isDirty;
    private string remarks;
    private string @return;
    private string shortId;
    private string summary;

    public DocItem()
    {
        names.CollectionChanged += OnCollectionChanged;
        items.CollectionChanged += OnCollectionChanged;
        seeAlso.CollectionChanged += OnCollectionChanged;
    }

    public string ShortId
    {
        get => shortId;
        set
        {
            if (shortId == value) return;

            shortId = value;
            IsDirty = true;
        }
    }

    public IList<string> Names => names;

    public string Summary
    {
        get => summary;
        set
        {
            if (summary == value) return;

            summary = value;
            IsDirty = true;
        }
    }

    public string Remarks
    {
        get => remarks;
        set
        {
            if (remarks == value) return;

            remarks = value;
            IsDirty = true;
        }
    }

    public string Return
    {
        get => @return;
        set
        {
            if (@return == value) return;

            @return = value;
            IsDirty = true;
        }
    }

    public IList<IDocSubItem> Items => items;

    public IList<string> SeeAlso => seeAlso;

    public bool IsDirty
    {
        get => isDirty ? isDirty : isDirty = items.Any(DirtyPredicate);
        set => isDirty = value;
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        IsDirty = true;
    }

    private static bool DirtyPredicate(IDocSubItem x) => x.IsDirty;
}