#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpGen.Platform;

public sealed class ObservableSet<T> : ObservableCollection<T>
{
    private readonly HashSet<T> set;

    public ObservableSet(IEqualityComparer<T> equalityComparer) => set = new HashSet<T>(equalityComparer);

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    protected override void InsertItem(int index, T item)
    {
        if (!set.Add(item))
            return;

        base.InsertItem(index, item);
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        set.Clear();
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];
        set.Remove(item);
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, T item)
    {
        if (!set.Add(item))
            return;

        var oldItem = this[index];
        set.Remove(oldItem);
        base.SetItem(index, item);
    }
}