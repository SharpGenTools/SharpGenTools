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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SharpGen.Config;

/// <summary>
/// A simple XML preprocessor.
/// </summary>
internal static class Preprocessor
{
    /// <summary>
    /// Preprocesses the XML document given the reader instance.
    /// </summary>
    public static void Preprocess(XmlReader xmlReader, Stream outputStream, params string[] macros)
    {
        var doc = XDocument.Load(xmlReader);

        XNamespace ns = ConfigFile.XmlNamespace;

        var list = doc.Descendants(ns + "ifndef").ToList();
        // Work on deepest first
        list.Reverse();
        foreach (var ifndef in list)
        {
            var attr = ifndef.Attribute("name");
            if (attr != null && macros.Contains(attr.Value))
            {
                ifndef.Remove();
            }
            else
            {
                foreach (var element in ifndef.Elements())
                {
                    ifndef.AddBeforeSelf(element);
                }

                ifndef.Remove();
            }
        }

        list.Clear();
        list.AddRange(doc.Descendants(ns + "ifdef"));
        // Work on deepest first
        list.Reverse();
        foreach (var ifdef in list)
        {
            var attr = ifdef.Attribute("name");
            if (attr != null && !string.IsNullOrWhiteSpace(attr.Value))
            {
                var values = attr.Value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Any(macros.Contains))
                {
                    foreach (var element in ifdef.Elements())
                    {
                        ifdef.AddBeforeSelf(element);
                    }
                }
            }

            ifdef.Remove();
        }

        doc.Save(outputStream);
    }
}