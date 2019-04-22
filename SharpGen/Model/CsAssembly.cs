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
using System.Xml;
using SharpGen.Logging;
using SharpGen.Config;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace SharpGen.Model
{
    /// <summary>
    /// An assembly container for namespaces.
    /// </summary>
    [DataContract(Name = "Assembly")]
    public class CsAssembly : CsBase
    {
        public CsAssembly()
        {
            Interop = new InteropManager();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsAssembly"/> class.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="appType">The application type this assembly is generated for. (Used for the check file)</param>
        public CsAssembly(string assemblyName)
            :this()
        {
            Name = assemblyName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is to update.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is to update; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool NeedsToBeUpdated { get; set; }
        
        /// <summary>
        /// Gets the name of the check file for this assembly.
        /// </summary>
        /// <value>The name of the check file.</value>
        public string CheckFileName
        {
            get { return QualifiedName + ".check"; }
        }

        /// <summary>
        /// Gets the namespaces.
        /// </summary>
        /// <value>The namespaces.</value>
        public IEnumerable<CsNamespace> Namespaces
        {
            get { return Items.OfType<CsNamespace>(); }
        }

        /// <summary>
        /// Gets or sets the interop associated with this AssemblyContainer.
        /// </summary>
        /// <value>The interop.</value>
        [DataMember]
        public InteropManager Interop { get; set; }

        /// <summary>
        /// Reads the module from the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>A C++ module</returns>
        public static CsAssembly Read(string file)
        {
            using (var input = new FileStream(file, FileMode.Open))
            {
                return Read(input);
            }
        }

        /// <summary>
        /// Reads the module from the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A C++ module</returns>
        public static CsAssembly Read(Stream input)
        {
            var ds = GetSerializer();

            using (XmlReader w = XmlReader.Create(input))
            {
                return ds.ReadObject(w) as CsAssembly;
            }
        }

        private static DataContractSerializer GetSerializer()
        {
            var knownTypes = new[]
            {
                        typeof(CsAssembly),
                        typeof(CsNamespace),
                        typeof(CsInterface),
                        typeof(CsGroup),
                        typeof(CsStruct),
                        typeof(CsInterfaceArray),
                        typeof(CsEnum),
                        typeof(CsEnumItem),
                        typeof(CsFunction),
                        typeof(CsMethod),
                        typeof(CsField),
                        typeof(CsParameter),
                        typeof(CsProperty),
                        typeof(CsVariable),
                        typeof(CsTypeBase),
                        typeof(CsReturnValue),
                        typeof(CsMarshalBase),
                        typeof(CsFundamentalType),
                        typeof(CsUndefinedType),
                        typeof(StructSizeRelation),
                        typeof(ConstantValueRelation),
                        typeof(LengthRelation)
            };

            return new DataContractSerializer(typeof(CsAssembly), new DataContractSerializerSettings
            {
                KnownTypes = knownTypes,
                PreserveObjectReferences = true
            });
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
            var ds = GetSerializer();

            var settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter w = XmlWriter.Create(output, settings))
            {
                ds.WriteObject(w, this);
            }
        }
    }
}