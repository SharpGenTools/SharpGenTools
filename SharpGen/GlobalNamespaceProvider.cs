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
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpGen;

/// <summary>
///     Global namespace provider.
/// </summary>
public class GlobalNamespaceProvider
{
    private readonly Dictionary<WellKnownName, string> wellKnownOverrides = new();
    private readonly Dictionary<WellKnownName, NameSyntax> wellKnownOverrideCache = new();
    private readonly Dictionary<WellKnownName, NameSyntax> wellKnownDefaults = new();

    private readonly Dictionary<BuiltinType, NameSyntax> builtInDefaults = new()
    {
        [BuiltinType.Marshal] = SyntaxFactory.ParseName("System.Runtime.InteropServices.Marshal"),
        [BuiltinType.Math] = SyntaxFactory.ParseName("System.Math"),
        [BuiltinType.Unsafe] = SyntaxFactory.ParseName("System.Runtime.CompilerServices.Unsafe"),
        [BuiltinType.Span] = SyntaxFactory.ParseName("System.Span"),
        [BuiltinType.GCHandle] = SyntaxFactory.ParseName("System.Runtime.InteropServices.GCHandle"),
        [BuiltinType.Delegate] = SyntaxFactory.ParseName("System.Delegate"),
        [BuiltinType.FlagsAttribute] = SyntaxFactory.ParseName("System.FlagsAttribute"),
        [BuiltinType.GC] = SyntaxFactory.ParseName("System.GC"),
        [BuiltinType.IntPtr] = SyntaxFactory.ParseName("System.IntPtr"),
        [BuiltinType.UIntPtr] = SyntaxFactory.ParseName("System.UIntPtr"),
        [BuiltinType.GuidAttribute] = SyntaxFactory.ParseName("System.Runtime.InteropServices.GuidAttribute"),
        [BuiltinType.StructLayoutAttribute] =
            SyntaxFactory.ParseName("System.Runtime.InteropServices.StructLayoutAttribute"),
        [BuiltinType.PlatformNotSupportedException] =
            SyntaxFactory.ParseName("System.PlatformNotSupportedException"),
        [BuiltinType.UnmanagedFunctionPointerAttribute] =
            SyntaxFactory.ParseName("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"),
        [BuiltinType.UnmanagedCallersOnlyAttribute] =
            SyntaxFactory.ParseName("System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"),
        [BuiltinType.Guid] = SyntaxFactory.ParseName("System.Guid"),
        // TODO: use these!
    };

    private readonly Dictionary<BuiltinType, string> builtInOverrides = new();
    private readonly Dictionary<BuiltinType, NameSyntax> builtInOverrideCache = new();

    public string GetTypeName(WellKnownName name) => wellKnownOverrides.TryGetValue(name, out var overridenName)
                                                         ? overridenName
                                                         : WellKnownDefaultName(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string WellKnownDefaultName(WellKnownName name) => name switch
    {
        WellKnownName.WinRTString => "SharpGen.Runtime.Win32.WinRTString",
        _ => "SharpGen.Runtime." + name
    };

    public NameSyntax GetTypeNameSyntax(WellKnownName name)
    {
        if (wellKnownOverrideCache.TryGetValue(name, out var cache))
            return cache;

        NameSyntax value;
        if (wellKnownOverrides.TryGetValue(name, out var overrideName))
        {
            value = SyntaxFactory.ParseName(overrideName);
            wellKnownOverrideCache.Add(name, value);
            return value;
        }

        if (wellKnownDefaults.TryGetValue(name, out var defaultName))
            return defaultName;

        value = SyntaxFactory.ParseName(WellKnownDefaultName(name));
        wellKnownDefaults.Add(name, value);
        return value;
    }

    public NameSyntax GetTypeNameSyntax(BuiltinType type)
    {
        if (builtInOverrideCache.TryGetValue(type, out var cache))
            return cache;

        if (builtInOverrides.TryGetValue(type, out var overrideName))
        {
            var value = SyntaxFactory.ParseName(overrideName);
            builtInOverrideCache.Add(type, value);
            return value;
        }

        return builtInDefaults[type];
    }

    public NameSyntax GetGenericTypeNameSyntax(BuiltinType type, TypeArgumentListSyntax typeArgumentList)
    {
        if (type != BuiltinType.Span)
            throw new ArgumentOutOfRangeException(nameof(type));

        return SyntaxFactory.QualifiedName(
            SyntaxFactory.IdentifierName("System"),
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("Span")).WithTypeArgumentList(typeArgumentList)
        );
    }

    public void OverrideName(WellKnownName wellKnownName, string name) => wellKnownOverrides[wellKnownName] = name;
    public void OverrideName(BuiltinType builtInName, string name) => builtInOverrides[builtInName] = name;
}