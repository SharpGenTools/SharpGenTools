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

namespace SharpGen.CppModel;

/// <summary>
///   Base class for all C++ element.
/// </summary>
public abstract class CppElement
{
    private MappingRule rule;

    protected CppElement(string name)
    {
        Name = name;
    }

    public string Name { get; }

#nullable enable
    public CppContainer? Parent { get; internal set; }

    public MappingRule Rule => rule ??= new MappingRule();

    public CppInclude? ParentInclude
    {
        get
        {
            var cppInclude = Parent;
            while (cppInclude is { } and not CppInclude)
                cppInclude = cppInclude.Parent;
            return cppInclude as CppInclude;
        }
    }

    public void RemoveFromParent()
    {
        var parent = Parent;
        if (parent == null)
            return;

        parent.RemoveChild(this);

        Parent = null;
    }
#nullable restore

    private protected virtual string Path => Parent != null ? Parent.FullName : string.Empty;

    public virtual string FullName
    {
        get
        {
            var path = Path;
            return string.IsNullOrEmpty(path) ? Name : path + "::" + Name;
        }
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => GetType().Name + " [" + Name + "]";
}