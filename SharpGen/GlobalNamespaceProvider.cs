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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace SharpGen
{
    public enum BuiltinType
    {
        Marshal
    }

    public enum WellKnownName
    {
        Result,
        FunctionCallback,
        PointerSize,
        CppObject,
        ICallbackable,
        BooleanHelpers,
        MemoryHelpers,
        StringHelpers,
        InterfaceArray,
    }

    /// <summary>
    /// Global namespace provider.
    /// </summary>
    public class GlobalNamespaceProvider
    {
        private readonly Dictionary<WellKnownName, string> overrides = new Dictionary<WellKnownName, string>();

        private string Name { get; }

        public GlobalNamespaceProvider(string name)
        {
            Name = name;
        }

        private string GetLocalName(WellKnownName name)
        {
            return overrides.TryGetValue(name, out var overridenName) ? overridenName : name.ToString();
        }
        
        public string GetTypeName(WellKnownName name)
        {
            return $"{Name}.{GetLocalName(name)}";
        }

        public QualifiedNameSyntax GetTypeNameSyntax(WellKnownName name)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(Name),
                SyntaxFactory.IdentifierName(GetLocalName(name)));
        }

        public NameSyntax GetTypeNameSyntax(BuiltinType type)
        {
            switch (type)
            {
                case BuiltinType.Marshal:
                    return SyntaxFactory.ParseName("System.Runtime.InteropServices.Marshal");
                default:
                    return null;
            }
        }

        public void OverrideName(WellKnownName wellKnownName, string name)
        {
            overrides[wellKnownName] = name;
        }
    }
}