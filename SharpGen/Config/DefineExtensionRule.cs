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

using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace SharpGen.Config;

[XmlType("define")]
public class DefineExtensionRule : ExtensionBaseRule
{
    [XmlAttribute("enum")]
    public string Enum { get; set; }
    [XmlAttribute("struct")]
    public string Struct { get; set; }
    [XmlAttribute("interface")]
    public string Interface { get; set; }

    [XmlAttribute("native")]
    public string NativeImplementation { get; set; }

    [XmlAttribute("underlying")]
    public string UnderlyingType { get; set; }

    [XmlAttribute("shadow"), DefaultValue(null)]
    public string ShadowName { get; set; }

    [XmlAttribute("vtbl")]
    public string VtblName { get; set; }

    [XmlIgnore] public int? SizeOf { get; set; }

    [XmlAttribute("sizeof")]
    public int _SizeOf_
    {
        get => SizeOf.Value;
        set => SizeOf = value;
    }

    public bool ShouldSerialize_SizeOf_() => SizeOf != null;

    [XmlIgnore] public int? Align { get; set; }

    [XmlAttribute("align")]
    public int _Align_
    {
        get => Align.Value;
        set => Align = value;
    }

    public bool ShouldSerialize_Align_() => Align != null;

    [XmlIgnore] public bool? HasCustomMarshal { get; set; }

    [XmlAttribute("marshal")]
    public bool _HasCustomMarshal_
    {
        get => HasCustomMarshal.Value;
        set => HasCustomMarshal = value;
    }

    public bool ShouldSerialize_HasCustomMarshal_() => HasCustomMarshal != null;

    [XmlIgnore] public bool? IsStaticMarshal { get; set; }

    [XmlAttribute("static-marshal")]
    public bool _IsStaticMarshal_
    {
        get => IsStaticMarshal.Value;
        set => IsStaticMarshal = value;
    }

    public bool ShouldSerialize_IsStaticMarshal_() => IsStaticMarshal != null;

    [XmlIgnore] public bool? HasCustomNew { get; set; }

    [XmlAttribute("custom-new")]
    public bool _HasCustomNew_
    {
        get => HasCustomNew.Value;
        set => HasCustomNew = value;
    }

    public bool ShouldSerialize_HasCustomNew_() => HasCustomNew != null;

    [XmlIgnore] public bool? IsNativePrimitive { get; set; }

    [XmlAttribute("primitive")]
    public bool _IsNativePrimitive_
    {
        get => IsNativePrimitive.Value;
        set => IsNativePrimitive = value;
    }

    public bool ShouldSerialize_IsNativePrimitive_() => IsNativePrimitive != null;

    [XmlIgnore] public bool? IsCallbackInterface { get; set; }

    [XmlAttribute("callback")]
    public bool _IsCallbackInterface_
    {
        get => IsCallbackInterface.Value;
        set => IsCallbackInterface = value;
    }

    public bool ShouldSerialize_IsCallbackInterface_() => IsCallbackInterface != null;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", base.ToString(), SizeOf.HasValue ? "sizeof:" + SizeOf.Value : "", Align.HasValue ? "align:" + Align.Value : "");
    }
}