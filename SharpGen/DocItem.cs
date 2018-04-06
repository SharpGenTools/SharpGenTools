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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SharpGen
{
    [XmlRoot("documentation", Namespace = NS)]
    public class DocItemCache
    {
        private const string NS = "urn:SharpGen.DocCache";

        private readonly object syncObject = new object();

        public List<DocItem> DocItems { get; set; } = new List<DocItem>();

        public void Add(DocItem item)
        {
            lock (syncObject)
            {
                DocItems.Add(item);
            }
        }

        public DocItem Find(string name)
        {
            lock (syncObject)
            {
                return DocItems.FirstOrDefault(item => item.Name == name);
            }
        }

        public static DocItemCache Read(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return Read(stream);
            }
        }

                /// <summary>
        /// Reads the module from the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A C++ module</returns>
        public static DocItemCache Read(Stream input)
        {
            var ds = new XmlSerializer(typeof (DocItemCache));

            DocItemCache module = null;
            using (XmlReader w = XmlReader.Create(input))
            {
                module = ds.Deserialize(w) as DocItemCache;
            }
            return module;
        }

        /// <summary>
        /// Writes this instance to the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        public void Write(string file)
        {
            using (var output = new FileStream(file, FileMode.Create))
            {
                Write(output); 
            }
        }

        /// <summary>
        /// Writes this instance to the specified output.
        /// </summary>
        /// <param name="output">The output.</param>
        public void Write(Stream output)
        {
            var ds = new XmlSerializer(typeof (DocItemCache));

            var settings = new XmlWriterSettings {Indent = true};
            using (XmlWriter w = XmlWriter.Create(output, settings))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", NS);
                ds.Serialize(w, this, ns);
            }
        }
    }

    /// <summary>
    /// Documentation item
    /// </summary>
    [XmlType("doc-item")]
    public class DocItem
    {
        public DocItem()
        {
            Items = new List<DocSubItem>();
        }

        /// <summary>
        /// Gets or sets the short id.
        /// </summary>
        /// <value>
        /// The short id.
        /// </value>
        [XmlAttribute("short-id")]
        public string ShortId { get; set; }

        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>The summary.</value>
        [XmlElement("summary")]
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the remarks.
        /// </summary>
        /// <value>The remarks.</value>
        [XmlElement("remarks")]
        public string Remarks { get; set; }

        /// <summary>
        /// Gets or sets the return.
        /// </summary>
        /// <value>The return.</value>
        [XmlElement("return")]
        public string Return { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public List<DocSubItem> Items { get; set; }
    }

    /// <summary>
    /// Documentation sub-item, used for structure fields, enum items, and function parameters.
    /// </summary>
    [XmlType("sub-item")]
    public class DocSubItem
    {
        /// <summary>
        /// Gets or sets the name of the sub item.
        /// </summary>
        [XmlAttribute("term")]
        public string Term { get; set; }

        /// <summary>
        /// Gets or sets the description associated with the sub item.
        /// </summary>
        [XmlElement("description")]
        public string Description { get; set; }
    }
}