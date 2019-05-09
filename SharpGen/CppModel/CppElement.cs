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
using SharpGen.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SharpGen.CppModel
{
    /// <summary>
    ///   Base class for all C++ element.
    /// </summary>
    public class CppElement
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        [XmlElement("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [XmlElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the remarks.
        /// </summary>
        /// <value>The remarks.</value>
        [XmlElement("remarks")]
        public string Remarks { get; set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [XmlIgnore]
        public CppElement Parent { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        [XmlIgnore]
        public MappingRule Rule { get; set; }

        /// <summary>
        /// Gets the parent include.
        /// </summary>
        /// <value>The parent include.</value>
        [XmlIgnore]
        public CppInclude ParentInclude
        {
            get
            {
                var cppInclude = Parent;
                while (cppInclude != null && !(cppInclude is CppInclude))
                    cppInclude = cppInclude.Parent;
                return cppInclude as CppInclude;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        [XmlIgnore]
        public virtual string Path
        {
            get
            {
                if (Parent != null)
                    return Parent.FullName;
                return "";
            }
        }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <value>The full name.</value>
        [XmlIgnore]
        public virtual string FullName
        {
            get
            {
                string path = Path;
                string name = Name ?? "";
                return String.IsNullOrEmpty(path) ? name : path + "::" + name;
            }
        }

        /// <summary>
        /// Return all items inside this C++ element.
        /// </summary>
        [XmlArray("items")]
        [XmlArrayItem(typeof (CppConstant))]
        [XmlArrayItem(typeof (CppDefine))]
        [XmlArrayItem(typeof (CppEnum))]
        [XmlArrayItem(typeof (CppEnumItem))]
        [XmlArrayItem(typeof (CppField))]
        [XmlArrayItem(typeof (CppFunction))]
        [XmlArrayItem(typeof (CppGuid))]
        [XmlArrayItem(typeof (CppInclude))]
        [XmlArrayItem(typeof (CppInterface))]
        [XmlArrayItem(typeof (CppMethod))]
        [XmlArrayItem(typeof (CppParameter))]
        [XmlArrayItem(typeof (CppStruct))]
        [XmlArrayItem(typeof(CppReturnValue))]
        [XmlArrayItem(typeof(CppMarshallable))]
        public List<CppElement> Items { get; set; }

        protected internal virtual IEnumerable<CppElement> AllItems
        {
            get { return Iterate<CppElement>(); }
        }

        [XmlIgnore]
        public bool IsEmpty
        {
            get { return Items == null || Items.Count == 0; }
        }

        public MappingRule GetMappingRule()
        {
            return Rule ?? (Rule = new MappingRule());
        }

        /// <summary>
        ///   Add an inner element to this CppElement
        /// </summary>
        /// <param name = "element"></param>
        public void Add(CppElement element)
        {
            if (element.Parent != null)
                element.Parent.Remove(element);
            element.Parent = this;
            if (Items == null)
            {
                Items = new List<CppElement>();
            }

            Items.Add(element);
        }

        /// <summary>
        ///   Remove an inner element to this CppElement
        /// </summary>
        /// <param name = "element"></param>
        public void Remove(CppElement element)
        {
            element.Parent = null;
            if (Items != null)
            {
                Items.Remove(element);
            }
        }

        /// <summary>
        ///   Iterates on items on this instance.
        /// </summary>
        /// <typeparam name = "T">Type of the item to iterate</typeparam>
        /// <returns>An enumeration on items</returns>
        public IEnumerable<T> Iterate<T>() where T : CppElement
        {
            return Items == null ? Enumerable.Empty<T>() : Items.OfType<T>();
        }

        protected void ResetParents()
        {
            foreach (var innerElement in Items)
            {
                innerElement.Parent = this;
                innerElement.ResetParents();
            }
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return GetType().Name + " [" + Name + "]";
        }
        
        public virtual string ToShortString()
        {
            return Name;
        }
    }
}