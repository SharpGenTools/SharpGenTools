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
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Generator;
using SharpGen.Transform;

namespace SharpGen.Model
{
    [DataContract]
    public class CsMethod : CsCallable
    {
        protected override int MaxSizeReturnParameter => 4;

        public CsMethod()
        {
        }

        public CsMethod(CppMethod cppMethod)
            :base(cppMethod)
        {
        }

        [DataMember]
        public bool Hidden { get; set; }

        public override void FillDocItems(IList<string> docItems, IDocumentationLinker manager)
        {
            foreach (var param in PublicParameters)
                docItems.Add("<param name=\"" + param.Name + "\">" + manager.GetSingleDoc(param) + "</param>");

            if (HasReturnType)
                docItems.Add("<returns>" + GetReturnTypeDoc(manager) + "</returns>");
        }

        protected override void UpdateFromMappingRule(MappingRule tag)
        {
            base.UpdateFromMappingRule(tag);

            AllowProperty = !tag.Property.HasValue || tag.Property.Value;

            IsPersistent = tag.Persist.HasValue && tag.Persist.Value;

            if(tag.CustomVtbl.HasValue)
                CustomVtbl = tag.CustomVtbl.Value;
        }

        [DataMember]
        public bool AllowProperty { get; set; }

        [DataMember]
        public bool CustomVtbl { get; set; }

        [DataMember]
        public bool IsPersistent { get; set; }

        [DataMember]
        public int Offset { get; set; }

        [DataMember]
        public int WindowsOffset { get; set; }
    }
}