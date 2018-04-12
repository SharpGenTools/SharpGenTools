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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Generator;
using System.Diagnostics;
using SharpGen.Transform;
using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace SharpGen.Model
{
    /// <summary>
    /// Root class for all model elements.
    /// </summary>
    [DebuggerDisplay("Name: {Name}")]
    [DataContract(Name = "Element")]
    public class CsBase
    {
        private ObservableCollection<CsBase> _items;
        private CppElement _cppElement;
        private string _cppElementName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsBase"/> class.
        /// </summary>
        public CsBase()
        {
            Visibility = Visibility.Public;
            IsFullyMapped = true;
            Description = "No documentation.";
            Remarks = "";
        }

        private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<CsBase>())
                {
                    item.Parent = this;
                } 
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<CsBase>())
                {
                    item.Parent = null;
                } 
            }
        }

        /// <summary>
        /// Gets or sets the parent of this container.
        /// </summary>
        /// <value>The parent.</value>
        [DataMember]
        public CsBase Parent { get; set; }
        
        /// <summary>
        /// Gets the parent of a specified type. This method goes back
        /// to all parent and returns the first parent of the type T or null if no parent were found.
        /// </summary>
        /// <typeparam name="T">Type of the parent</typeparam>
        /// <returns>a valid reference to the parent T or null if no parent of this type</returns>
        public T GetParent<T>() where T : CsBase
        {
            var parent = Parent;
            while (parent != null && !(parent is T))
                parent = parent.Parent;
            return (T) parent;
        }

        /// <summary>
        /// Gets items stored in this container.
        /// </summary>
        /// <value>The items.</value>
        [DataMember]
        public ObservableCollection<CsBase> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new ObservableCollection<CsBase>();
                    _items.CollectionChanged += ItemsChanged;
                }
                return _items;
            }
        }

        protected void ResetItems()
        {
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
            Items.Add(innerCs);
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
            Items.Remove(innerCs);
        }

        /// <summary>
        /// Gets or sets the name of this element.
        /// </summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Visibility"/> of this element. Default is public.
        /// </summary>
        /// <value>The visibility.</value>
        [DataMember]
        public Visibility Visibility { get; set; }

        /// <summary>
        /// Returns a textual representation of the <see cref="Visibility"/> property.
        /// </summary>
        /// <value>The full name of the visibility.</value>
        public string VisibilityName
        {
            get
            {
                var builder = new StringBuilder();

                if ((Visibility & Visibility.Public) != 0)
                    builder.Append("public ");
                else if ((Visibility & Visibility.Protected) != 0)
                    builder.Append("protected ");
                else if ((Visibility & Visibility.Internal) != 0)
                    builder.Append("internal ");
                else if ((Visibility & Visibility.Private) != 0)
                    builder.Append("private ");
                else if ((Visibility & Visibility.PublicProtected) != 0)
                    builder.Append("");

                if ((Visibility & Visibility.Const) != 0)
                    builder.Append("const ");

                if ((Visibility & Visibility.Static) != 0)
                    builder.Append("static ");

                if ((Visibility & Visibility.Sealed) != 0)
                    builder.Append("sealed ");

                if ((Visibility & Visibility.Readonly) != 0)
                    builder.Append("readonly ");

                if ((Visibility & Visibility.Override) != 0)
                    builder.Append("override ");

                if ((Visibility & Visibility.Abstract) != 0)
                    builder.Append("abstract ");

                if ((Visibility & Visibility.Virtual) != 0)
                    builder.Append("virtual ");

                return builder.ToString();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is already fully mapped.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is fully mapped; otherwise, <c>false</c>.
        /// </value>
        public bool IsFullyMapped { get; set; }

        /// <summary>
        /// Gets the full qualified name of this type.
        /// </summary>
        /// <value>The full name.</value>
        public virtual string QualifiedName
        {
            get
            {
                string path = Parent?.QualifiedName;
                string name = Name ?? "";
                return string.IsNullOrEmpty(path) ? name : path + "." + name;
            }
        }

        /// <summary>
        /// Gets or sets the C++ element associated to this container.
        /// </summary>
        /// <value>The C++ element.</value>
        public virtual CppElement CppElement
        {
            get { return _cppElement; }
            set
            {
                _cppElement = value;
                if (_cppElement != null )
                {
                    DocId = string.IsNullOrEmpty(CppElement.Id) ? DocId : CppElement.Id;
                    Description = string.IsNullOrEmpty(CppElement.Description) ? Description : CppElement.Description;
                    Remarks = string.IsNullOrEmpty(CppElement.Remarks) ? Remarks : CppElement.Remarks;
                    UpdateFromMappingRule(_cppElement.GetMappingRule());
                }
            }
        }

        /// <summary>
        /// Gets the name of the C++ element.
        /// </summary>
        /// <value>The name of the C++ element.</value>
        [DataMember(Name = "CppElement")]
        public string CppElementName
        {
            get
            {
                if (!string.IsNullOrEmpty(_cppElementName))
                    return _cppElementName;
                return CppElement?.Name;
            } 
            set { _cppElementName = value; }
        }

        /// <summary>
        /// Gets or sets the doc id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        [DataMember]
        public string DocId { get; set; }

        /// <summary>
        /// Gets or sets the description documentation.
        /// </summary>
        /// <value>The description.</value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the remarks documentation.
        /// </summary>
        /// <value>The remarks.</value>
        [DataMember]
        public string Remarks { get; set; }
        
        public virtual void FillDocItems(IList<string> docItems, IDocumentationLinker manager) {}
        
        public virtual string DocUnmanagedName
        {
            get { return CppElementName; }
        }

        public virtual string DocUnmanagedShortName
        {
            get { return CppElementName; }
        }

        /// <summary>
        /// Updates this element from a tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        protected virtual void UpdateFromMappingRule(MappingRule tag)
        {
            if (tag.Visibility.HasValue)
                Visibility = tag.Visibility.Value;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return QualifiedName;
        }
    }
}