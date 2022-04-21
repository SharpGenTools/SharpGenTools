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

namespace SharpGen.Model;

/// <summary>
/// A Namespace container.
/// </summary>
public sealed class CsNamespace : CsBase
{
    public CsNamespace(string nameSpace) : base(null, nameSpace)
    {
    }

    /// <summary>
    /// Gets the full qualified name of this type. This is the name of this assembly
    /// and equals to <see cref="CsBase.Name"/> property.
    /// </summary>
    /// <value>The full name.</value>
    public override string QualifiedName => Name;

    /// <summary>
    /// Gets all declared enums from this namespace.
    /// </summary>
    /// <value>The enums.</value>
    public IEnumerable<CsEnum> Enums => Items.OfType<CsEnum>();

    /// <summary>
    /// Gets all declared structs from this namespace.
    /// </summary>
    /// <value>The structs.</value>
    public IEnumerable<CsStruct> Structs => Items.OfType<CsStruct>();

    /// <summary>
    /// Gets all declared interfaces from this namespace.
    /// </summary>
    /// <value>The interfaces.</value>
    public IEnumerable<CsInterface> Interfaces => Items.OfType<CsInterface>();

    /// <summary>
    /// Gets all declared classes from this namespace.
    /// </summary>
    /// <value>The function groups.</value>
    public IEnumerable<CsGroup> Classes => Items.OfType<CsGroup>();
}