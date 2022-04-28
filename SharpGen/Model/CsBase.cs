// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Transform;

namespace SharpGen.Model;

/// <summary>
/// Root class for all model elements.
/// </summary>
[DebuggerDisplay("Name: {" + nameof(Name) + "}")]
public abstract class CsBase
{
    internal const string DefaultNoDescription = "No documentation.";
    private ObservableCollection<CsBase> _items;
    private string _cppElementName;
    private string description;
    private Visibility? _visibility;

    protected CsBase(CppElement cppElement, string name)
    {
        CppElement = cppElement;
        Name = name;

        if (cppElement is { Rule.Visibility: { } visibility })
            Visibility = visibility;
    }

    private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<CsBase>())
            {
                item.Parent = null;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<CsBase>())
            {
                item.Parent = this;
            }
        }

        OnItemsChanged();
        ExpireOnItemsChange();
    }

    private void ExpireOnItemsChange()
    {
        if (_items == null)
            return;

        foreach (var itemList in ExpiringOnItemsChange)
            itemList?.Expire();
    }

    protected virtual void OnItemsChanged()
    {
    }

    /// <summary>
    /// Gets or sets the parent of this container.
    /// </summary>
    /// <value>The parent.</value>
    public CsBase Parent { get; private set; }

    /// <summary>
    /// Gets the parent of a specified type. This method goes back
    /// to all parent and returns the first parent of the type T or null if no parent were found.
    /// </summary>
    /// <typeparam name="T">Type of the parent</typeparam>
    /// <returns>a valid reference to the parent T or null if no parent of this type</returns>
    public T GetParent<T>() where T : CsBase
    {
        var parent = Parent;
        while (parent is { } and not T)
            parent = parent.Parent;
        return (T) parent;
    }

    /// <summary>
    /// Gets items stored in this container.
    /// </summary>
    /// <value>The items.</value>
    public IReadOnlyCollection<CsBase> Items => ItemsImpl;

    private ObservableCollection<CsBase> ItemsImpl
    {
        get
        {
            if (_items == null) ResetItems();

            return _items;
        }
    }

    public virtual IEnumerable<CsBase> AdditionalItems => Enumerable.Empty<CsBase>();

    protected void ResetItems()
    {
        ExpireOnItemsChange();
        _items = new ObservableCollection<CsBase>();
        _items.CollectionChanged += ItemsChanged;
    }

    /// <summary>
    /// Adds the specified inner container to this container.
    /// </summary>
    /// <remarks>
    /// The Parent property of the innerContainer is set to this container.
    /// </remarks>
    /// <param name="innerCs">The inner container.</param>
    public void Add(CsBase innerCs)
    {
        ItemsImpl.Add(innerCs);
    }

    /// <summary>
    /// Removes the specified inner container from this container
    /// </summary>
    /// <remarks>
    /// The Parent property of the innerContainer is set to null.
    /// </remarks>
    /// <param name="innerCs">The inner container.</param>
    public void Remove(CsBase innerCs)
    {
        ItemsImpl.Remove(innerCs);
    }

    /// <summary>
    /// Gets or sets the name of this element.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; private protected set; }

    /// <summary>
    /// Gets or sets the <see cref="Visibility"/> of this element. Default is public.
    /// </summary>
    /// <value>The visibility.</value>
    public Visibility Visibility
    {
        get => _visibility ?? DefaultVisibility;
        set => _visibility = value;
    }

    public SyntaxTokenList VisibilityTokenList => ModelUtilities.VisibilityToTokenList(Visibility);

    protected virtual Visibility DefaultVisibility => Visibility.Public;

    /// <summary>
    /// Gets the full qualified name of this type.
    /// </summary>
    /// <value>The full name.</value>
    public virtual string QualifiedName
    {
        get
        {
            var path = Parent?.QualifiedName;
            var name = Name ?? string.Empty;
            return string.IsNullOrEmpty(path) ? name : path + "." + name;
        }
    }

    /// <summary>
    /// Gets or sets the C++ element associated to this container.
    /// </summary>
    /// <value>The C++ element.</value>
    public CppElement CppElement { get; }

    public string CppElementName
    {
        get => string.IsNullOrEmpty(_cppElementName) ? CppElement?.Name : _cppElementName;
        set => _cppElementName = value;
    }

    public string CppElementFullName => CppElement?.FullName ?? CppElementName;

    /// <summary>
    /// Gets or sets the doc id.
    /// </summary>
    public string DocId { get; set; }

    /// <summary>
    /// Gets or sets the description documentation.
    /// </summary>
    public virtual string Description
    {
        get => string.IsNullOrEmpty(description) ? DefaultDescription : description;
        set => description = value;
    }

    protected virtual string DefaultDescription => DefaultNoDescription;

    /// <summary>
    /// Gets or sets the remarks documentation.
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    public virtual void FillDocItems(IList<string> docItems, IDocumentationLinker manager)
    {
    }

    public virtual string DocUnmanagedName => CppElementFullName;

    public virtual string DocUnmanagedShortName => CppElementName;

    [ExcludeFromCodeCoverage]
    public override string ToString() => QualifiedName;

    protected void ResetParentAfterClone()
    {
        Parent = null;
    }

    private protected virtual IEnumerable<IExpiring> ExpiringOnItemsChange => Enumerable.Empty<IExpiring>();

    private protected static IEnumerable<CsBase> AppendNonNull(IEnumerable<CsBase> source, params CsBase[] values)
        => source.Concat(values.Where(x => x != null).Distinct());
}