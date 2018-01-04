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
        private List<ConfigFile> _configFilesLinked;

        public CsAssembly()
        {
            Interop = new InteropManager();
            _configFilesLinked = new List<ConfigFile>();
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
        /// Gets config files linked to this assembly
        /// </summary>
        /// <value>The config files linked to this assembly.</value>
        public IReadOnlyList<ConfigFile> ConfigFilesLinked => _configFilesLinked;

        /// <summary>
        /// Adds linked config file to this instance.
        /// </summary>
        /// <param name="configFileToAdd">The config file to add.</param>
        public void AddLinkedConfigFile(ConfigFile configFileToAdd)
        {
            foreach (var configFile in _configFilesLinked)
                if (configFile.Id == configFileToAdd.Id)
                    return;

            _configFilesLinked.Add(configFileToAdd);
        }

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
    }
}