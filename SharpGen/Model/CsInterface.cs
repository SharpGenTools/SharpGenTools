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
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Model
{
    [DataContract(Name = "Interface")]
    public class CsInterface : CsTypeBase
    {
        [ExcludeFromCodeCoverage(Reason = "Required for XML serialization.")]
        public CsInterface() : this(null)
        {
        }

        public CsInterface(CppInterface cppInterface)
        {
            CppElement = cppInterface;
            if (cppInterface != null)
                Guid = cppInterface.Guid;
        }

        public IEnumerable<CsMethod> Methods
        {
            get { return Items.OfType<CsMethod>(); }
        }

        public IEnumerable<CsProperty> Properties
        {
            get { return Items.OfType<CsProperty>(); }
        }

        /// <summary>
        /// Gets the variables stored in this container.
        /// </summary>
        /// <value>The variables.</value>
        public IEnumerable<CsVariable> Variables
        {
            get { return Items.OfType<CsVariable>(); }
        }

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);
            IsCallback = tag.IsCallbackInterface ?? false;
            IsDualCallback = tag.IsDualCallbackInterface ?? false;
            AutoGenerateShadow = tag.AutoGenerateShadow ?? false;
            ShadowName = tag.ShadowName;
            VtblName = tag.VtblName;
        }

        /// <summary>
        /// Class Parent inheritance
        /// </summary>
        [DataMember]
        public CsInterface Base { get; set; }

        /// <summary>
        /// Interface Parent inheritance
        /// </summary>
        [DataMember]
        public CsInterface IBase { get; set; }

        [DataMember]
        public CsInterface NativeImplementation { get; set; }

        [DataMember]
        public string Guid { get; set; }

        /// <summary>
        ///   Only valid for inner interface. Specify the name of the property in the outer interface to access to the inner interface
        /// </summary>
        [DataMember]
        public string PropertyAccessName { get; set; }

        /// <summary>
        ///   True if this interface is used as a callback to a C# object
        /// </summary>
        [DataMember]
        public bool IsCallback { get; set; }

        /// <summary>
        ///   True if this interface is used as a dual-callback to a C# object
        /// </summary>
        [DataMember]
        public bool IsDualCallback { get; set; }

        private string shadowName;

        [DataMember]
        public string ShadowName
        {
            get => shadowName ?? $"{QualifiedName}Shadow";
            set => shadowName = value;
        }

        private string vtblName;

        [DataMember]
        public string VtblName
        {
            get => vtblName ?? $"{ShadowName}.{Name}Vtbl";
            set => vtblName = value;
        }

        [DataMember]
        public bool AutoGenerateShadow { get; set; }

        /// <summary>
        ///   List of declared inner structs
        /// </summary>
        public bool HasInnerInterfaces => Items.OfType<CsInterface>().Any();

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "csinterface {0} => {1}", CppElementName, QualifiedName);
        }

        /// <summary>
        ///   List of declared inner structs
        /// </summary>
        public IEnumerable<CsInterface> InnerInterfaces
        {
            get { return Items.OfType<CsInterface>(); }
        }
    }
}